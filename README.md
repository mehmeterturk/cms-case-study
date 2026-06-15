# CMS Mikroservisleri

Bir içerik yönetim sistemi (CMS) için iki bağımsız mikroservisten oluşan örnek proje:

- **User Service** — kullanıcı yönetimi (CRUD)
- **Content Service** — içerik yönetimi (CRUD); içerik oluştururken sahip kullanıcıyı
  User Service üzerinden senkron REST çağrısı ile doğrular.

## Teknolojiler

| Alan | Teknoloji |
|------|-----------|
| Dil / Runtime | C# / .NET 10 |
| Mimari | Servis başına Clean Architecture (Domain / Application / Infrastructure / Api) |
| Veritabanı | PostgreSQL (her servis için ayrı veritabanı), EF Core + Npgsql, code-first migration |
| Servisler arası iletişim | RESTful (HttpClient) + dayanıklılık: retry / timeout / circuit breaker (`Microsoft.Extensions.Http.Resilience`, Polly v8 tabanlı) |
| Doğrulama | FluentValidation |
| API dokümantasyonu | Swagger / OpenAPI (Swashbuckle) |
| Testler | xUnit + Moq |
| Konteynerleştirme | Docker + Docker Compose |

## Mimari

```
┌────────────────────┐   GET /users/{id}   ┌────────────────────┐
│  Content Service   │ ──────────────────▶ │   User Service     │
│  localhost:8080    │   (REST + Polly)    │   localhost:8081   │
└─────────┬──────────┘                     └─────────┬──────────┘
          │                                          │
    ┌─────▼──────┐                            ┌──────▼─────┐
    │ contentdb  │                            │   userdb   │
    │ PostgreSQL │                            │ PostgreSQL │
    │ :5433      │                            │ :5432      │
    └────────────┘                            └────────────┘
```

İçerik oluşturulurken (`POST /contents`) Content Service, istekteki `userId` için
User Service'e `GET /users/{id}` çağrısı yapar:
- Kullanıcı **varsa** içerik oluşturulur (`201`).
- Kullanıcı **yoksa** içerik oluşturulmaz (`400`).
- User Service'e **erişilemezse** retry'lar tükendikten sonra `502` döner.

## Proje yapısı

```
cms-microservices/
├── docker-compose.yml
├── user-service/
│   ├── Dockerfile
│   ├── UserService.slnx
│   ├── src/{Domain, Application, Infrastructure, Api}
│   └── tests/UserService.Tests
└── content-service/
    ├── Dockerfile
    ├── ContentService.slnx
    ├── src/{Domain, Application, Infrastructure, Api}
    └── tests/ContentService.Tests
```

Katman sorumlulukları:
- **Domain** — entity'ler ve ortak `BaseEntity` (Id, CreatedAt, UpdatedAt).
- **Application** — iş mantığı servisleri, DTO'lar, arayüzler, FluentValidation kuralları,
  uygulama istisnaları (`NotFoundException`, `UpstreamServiceException`).
- **Infrastructure** — EF Core `DbContext`, repository'ler, dayanıklı User Service HTTP client.
- **Api** — controller'lar, `Program.cs` (DI), Swagger, global hata yönetimi middleware'i, health check.

## Çalıştırma (Docker — önerilen)

```bash
docker compose up --build
```

Dört konteyner ayağa kalkar (userdb, contentdb, user-service, content-service).
Veritabanı şeması, her servisin açılışında otomatik migration ile oluşturulur.

| Servis | Swagger UI | Health |
|--------|-----------|--------|
| User Service | http://localhost:8081/swagger | http://localhost:8081/health |
| Content Service | http://localhost:8080/swagger | http://localhost:8080/health |

## API uç noktaları

**User Service** (`http://localhost:8081`)

| Metot | Yol | Açıklama |
|-------|-----|----------|
| GET | `/users` | Tüm kullanıcıları listele |
| POST | `/users` | Yeni kullanıcı oluştur |
| GET | `/users/{id}` | Belirli kullanıcıyı getir |
| PUT | `/users/{id}` | Kullanıcıyı güncelle |
| DELETE | `/users/{id}` | Kullanıcıyı sil |

**Content Service** (`http://localhost:8080`)

| Metot | Yol | Açıklama |
|-------|-----|----------|
| GET | `/contents` | İçerikleri listele (opsiyonel `?status=Draft\|Published\|Archived`) |
| POST | `/contents` | Yeni içerik oluştur (kullanıcıyı doğrular, taslak olarak başlar) |
| GET | `/contents/{id}` | Belirli içeriği getir |
| GET | `/contents/by-slug/{slug}` | Slug'a göre içerik getir |
| PUT | `/contents/{id}` | İçeriğin başlık/gövdesini güncelle |
| POST | `/contents/{id}/publish` | İçeriği yayına al (zaten yayındaysa 409) |
| POST | `/contents/{id}/archive` | İçeriği arşivle (zaten arşivliyse 409) |
| DELETE | `/contents/{id}` | İçeriği sil |

### İçerik modeli ve yayın yaşam döngüsü

Bir içerik; `title`, `body`, otomatik üretilen tekil `slug`, sahip `userId`, `status`
ve `publishedAt` alanlarından oluşur.

- **Slug**: Oluştururken başlıktan üretilir (Türkçe-duyarlı: "Merhaba Dünya" → `merhaba-dunya`),
  çakışırsa sonuna sayı eklenir (`merhaba-dunya-2`). İstekte `slug` verilirse o kullanılır.
  Güncellemede slug değişmez (kalıcı URL).
- **Durum geçişleri**: `Draft` → (publish) → `Published` → (archive) → `Archived`.
  Geçersiz geçişler 409 döner.

### Örnek akış

```bash
# 1) Kullanıcı oluştur
curl -s -X POST http://localhost:8081/users \
  -H "Content-Type: application/json" \
  -d '{"fullName":"Ada Lovelace","email":"ada@example.com"}'
# -> { "id": "....", ... }

# 2) Bu kullanıcı ile içerik oluştur (201 beklenir)
curl -s -X POST http://localhost:8080/contents \
  -H "Content-Type: application/json" \
  -d '{"title":"İlk içerik","body":"Merhaba dünya","userId":"<yukarıdaki-id>"}'

# 3) Var olmayan kullanıcı ile içerik oluştur (400 beklenir)
curl -s -X POST http://localhost:8080/contents \
  -H "Content-Type: application/json" \
  -d '{"title":"x","body":"y","userId":"00000000-0000-0000-0000-000000000000"}'
```

## Testler

```bash
# User Service
dotnet test user-service/UserService.slnx

# Content Service
dotnet test content-service/ContentService.slnx
```

Testler iş mantığını (servis katmanı) repository ve HTTP client mock'lanarak doğrular;
kritik akışlar: CRUD happy-path, NotFound, doğrulama hataları, kullanıcı doğrulama
(var / yok) ve User Service erişilemediğinde 502 davranışı.

## Yerel geliştirme (Docker'sız)

Her servis için `appsettings.json` içinde varsayılan bağlantı dizeleri tanımlıdır
(userdb: `localhost:5432`, contentdb: `localhost:5433`). Yerelde Postgres örnekleri
çalıştırıp `dotnet run --project src/<Servis>.Api` ile ayağa kaldırabilirsiniz.

## Kapsam dışı (bilinçli sadeleştirme)

Kimlik doğrulama/JWT, API Gateway ve mesaj kuyruğu (asenkron iletişim) bu vaka
kapsamı dışında bırakılmıştır; gerçek bir üretim ortamında eklenmesi önerilir.
