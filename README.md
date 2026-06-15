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
| Medya depolama | Değiştirilebilir sağlayıcı (port/adapter): Yerel disk (varsayılan), Amazon S3 (`AWSSDK.S3`), Azure Blob (`Azure.Storage.Blobs`) |
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

### Dayanıklılık (resilience) — retry + circuit breaker

`Microsoft.Extensions.Http.Resilience` (Polly v8) standart handler'ı, demo'da
gözlemlenebilir eşiklerle yapılandırıldı:

| Strateji | Ayar | Davranış |
|---|---|---|
| Retry | 2 tekrar, 500 ms backoff | Geçici hatada (5xx/408/429/bağlantı) tekrar dener |
| Circuit breaker | min 4 çağrı, %50 hata, 15 sn açık | Sürekli hatada devreyi açar; açıkken çağrı yapmadan hızlı reddeder |
| Timeout | deneme 5 sn / toplam 20 sn | Tek deneme ve tüm operasyon sınırı (HttpClient.Timeout kullanılmaz) |

**Gözlemlenebilir demo:** User Service durdurulup `POST /contents` çağrıldığında ilk birkaç
istek retry yapıp `502` döner; eşik aşılınca **circuit breaker açılır** ve sonraki istekler
**anında** `502` döner (çağrı bile yapılmaz). 15 sn sonra devre yarı-açık olur ve yeniden dener.
Devre açıkken oluşan `BrokenCircuitException` da `502`'ye çevrilir.

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
| POST | `/users` | Kullanıcı oluştur — **tek nesne** veya **dizi** (toplu, atomik) kabul eder |
| GET | `/users/{id}` | Belirli kullanıcıyı getir |
| PUT | `/users/{id}` | Kullanıcıyı güncelle |
| DELETE | `/users/{id}` | Kullanıcıyı sil |

> **Toplu kullanıcı ekleme:** `POST /users` aynı endpoint üzerinden hem tekil nesne
> hem de dizi kabul eder. Dizi gönderildiğinde işlem **atomiktir** — herhangi bir kayıt
> geçersizse (format hatası, batch içi veya veritabanındaki tekrar e-posta) **hiçbiri**
> eklenmez ve 400 döner.
>
> ```json
> [
>   { "fullName": "Ada Lovelace", "email": "ada@example.com" },
>   { "fullName": "Grace Hopper",      "email": "grace@example.com" }
> ]
> ```

**Content Service** (`http://localhost:8080`)

Medya, içeriğin bir parçasıdır: tüm GET yanıtları içeriğin `media` listesini içerir;
dosyalar içerik oluşturma/güncelleme sırasında aynı istekte yüklenir. Bu yüzden tek
bir `ContentsController` altında toplanmıştır.

| Metot | Yol | Açıklama |
|-------|-----|----------|
| GET | `/contents` | İçerikleri **medyalarıyla** listele (opsiyonel `?status=Draft\|Published\|Archived`, `?language=tr`) |
| GET | `/contents/{id}` | Belirli içeriği **medyalarıyla** getir |
| GET | `/contents/{id}/translations` | İçeriğin tüm dil versiyonlarını (aynı çeviri grubu) getir |
| GET | `/contents/languages` | Desteklenen dil kodlarını getir |
| GET | `/contents/by-slug/{slug}` | Slug'a göre içeriği **medyalarıyla** getir |
| POST | `/contents` | İçerik oluştur — **`multipart/form-data`**: `title`, `body`, `userId`, `language` (ISO 639-1, ör. `tr`), (ops.) `translationGroupId`, (ops.) `slug`, (ops.) `files` (çoklu, ≤25 MB). Kullanıcı doğrulanır, taslak başlar |
| PUT | `/contents/{id}` | İçeriği güncelle (**replace**) — **`multipart/form-data`**: `title`, `body`, (ops.) `files`. Gönderilen alanlar kaydı değiştirir; medya seti `files` ile **değiştirilir** (dosya gönderilmezse medya **tamamen kaldırılır**) |
| POST | `/contents/{id}/publish` | İçeriği yayına al (zaten yayındaysa 409) |
| POST | `/contents/{id}/archive` | İçeriği arşivle (zaten arşivliyse 409) |
| DELETE | `/contents/{id}` | İçeriği sil (ekli medya dosyaları da temizlenir) |
| GET | `/contents/{id}/media/{mediaId}/download` | Bir medya dosyasını indir |

> **PUT replace semantiği:** Güncellemede medya yönetimi tamamen `files` üzerinden yapılır
> (ayrı bir medya-silme endpoint'i yoktur). Yeni medya seti = gönderilen dosyalar; hiç dosya
> gönderilmezse içeriğin medyası temizlenir. Dosyalara erişim `.../download` ile sağlanır.

### Çoklu dil (localization)

İçerik çok dillidir ve bu, **ayrı bir servis/controller olmadan** Content Service içinde
modellenmiştir (dil, içeriğin bir özelliğidir; bağımsız bir bounded context değildir).

- Dil bir **enum**'dur (`Language`: `Tr`, `En`); böylece Swagger'da ve istemcide
  serbest metin yerine **seçilebilir bir liste** (dropdown) olarak sunulur.
  Geçersiz bir değer model binding aşamasında reddedilir (400); büyük/küçük harf duyarsızdır.
  Desteklenen diller `GET /contents/languages` ile alınabilir (`["Tr","En"]`).
- Yeni dil eklemek = enum'a değer eklemek (tek nokta).
- Aynı makalenin farklı dil versiyonları bir `translationGroupId` ile birbirine bağlanır.
  İlk versiyon yeni bir grup başlatır; çeviriler oluşturulurken bu grup kimliği verilir.
- **Kural**: Aynı çeviri grubunda bir dil yalnızca bir kez bulunabilir (unique index:
  `translationGroupId + language`). Olmayan bir gruba ekleme veya tekrar dil → 400.
- Her dil versiyonu **bağımsızdır**: kendi slug'ı, durumu (taslak/yayın) ve medyası olabilir.
- `GET /contents?language=tr` ile dile göre filtrelenir; `GET /contents/{id}/translations`
  ile bir içeriğin tüm dil versiyonları getirilir.

```bash
# TR versiyon (yeni grup başlatır)
curl -X POST .../contents -F "title=Merhaba Dünya" -F "body=..." -F "userId=$UID" -F "language=tr"
# -> translationGroupId: G1 döner

# Aynı makalenin EN versiyonu (G1 grubuna eklenir)
curl -X POST .../contents -F "title=Hello World" -F "body=..." -F "userId=$UID" -F "language=en" -F "translationGroupId=G1"
```

### Medya depolama soyutlaması (değiştirilebilir sağlayıcı)

Dosyaların fiziksel olarak nereye yazılacağı, Application katmanındaki tek bir port
(arayüz) ile soyutlanmıştır — bir "soket". Infrastructure katmanında bu sokete farklı
sağlayıcılar takılır; hangisinin kullanılacağı **yapılandırma ile** seçilir
(Dependency Inversion / Strategy deseni). Kod değişmeden depolama değişir.

```
Application:    IFileStorage (port / soket)
                      ▲
        ┌─────────────┼─────────────┐
Infrastructure:  LocalFileStorage  S3FileStorage  AzureBlobFileStorage
   (disk/volume)      (AWS S3)        (Azure Blob)
```

Sağlayıcı seçimi (`appsettings.json` veya ortam değişkeni):

```jsonc
"Storage": {
  "Provider": "Local",                 // Local | S3 | AzureBlob
  "Local":     { "RootPath": "/app/media" },
  "S3":        { "BucketName": "...", "Region": "eu-central-1" },
  "AzureBlob": { "ConnectionString": "...", "ContainerName": "..." }
}
```

Bu vaka **Local** sağlayıcı ile (Docker volume `content-media`) çalışır; bulut hesabı
gerektirmez. S3 veya Azure'a geçmek için yalnızca `Storage:Provider` ve ilgili ayarları
değiştirmek yeterlidir — DI yalnızca seçilen sağlayıcıyı örnekler.

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

## Varsayımlar ve Tasarım Kararları

Case bazı noktaları bilinçli olarak açık bırakmış. Aşağıda her belirsizlik için
verdiğim karar, gerekçesi ve üretim ortamındaki alternatifi belgelenmiştir.

### 1. Kullanıcı doğrulaması — varlık kontrolü (kimlik doğrulama değil)

"İçerik oluşturulurken kullanıcı bilgisinin doğrulanması" maddesini, içeriğe sahip
olarak atanan `userId`'nin User Service'te **var olup olmadığının** kontrolü olarak
yorumladım (existence validation). İçerik, doğrulanmamış bir kullanıcıyla oluşturulamaz.

- **Karar**: Content Service → `GET /users/{id}` (REST + Polly resilience).
- **Kapsam dışı**: İsteği yapanın kimliğinin/yetkisinin doğrulanması (login, JWT, rol/izin).
- **Üretimde**: API Gateway + JWT; her servis token'ı doğrular, içerik sahipliği yetkiyle eşleşir.

### 2. Referans bütünlüğü — kullanıcı silindiğinde içerikler

Servisler ayrı veritabanlarına sahip (database-per-service) olduğundan, çapraz servis
**senkron cascade** bilinçli olarak yapılmadı — User Service'in Content Service'i bilmesi
yanlış bir bağımlılık (coupling) doğururdu.

- **Karar**: Kullanıcı silindiğinde içerikler korunur (fiziksel silinmez); `userId`
  bir referanstır, çapraz-DB foreign key yoktur.
- **Üretimde**: `UserDeleted` domain event'i (outbox pattern + message broker) ile
  eventual consistency; Content Service olayı dinleyip ilgili içerikleri arşivler/soft-delete eder.

### 3. İçerik modeli — makale temelli + yayın yaşam döngüsü

İçeriği bir "makale/gönderi" olarak modelledim: başlık, gövde, tekil slug, sahip kullanıcı,
durum ve zaman damgaları. Buna bir yayın yaşam döngüsü (Draft → Published → Archived) eklendi.

- **Karar**: Backend prensiplerini (iş kuralları, durum geçişleri, tekillik) sergileyecek
  kadar zengin, ama gereksiz karmaşıklıktan uzak bir model.
- **Genişletilebilir**: Kategori, etiket, medya eki, içerik tipi, sürümleme — aynı mimariye eklenebilir.

### 4. Servisler arası iletişim — "kullanıcı güncelleme" örneği

Case'teki "İçerik servisi bir kullanıcıyı güncellemek için User Service'e çağrı yapabilir"
ifadesini, servislerin REST ile haberleştiğini **örnekleyen** illüstratif bir cümle olarak
okudum. İçerik servisinin kullanıcı güncellemesi anlamlı bir sorumluluk olmadığından
uygulanmadı; gerçek servisler arası çağrı, oluşturmada kullanıcı doğrulamasıdır.

### Diğer kapsam kararları

- **Listeleme**: Basit tutuldu (yalnızca `?status=` filtresi). Üretimde pagination + index'leme eklenir.
- **Silme**: İçerik için fiziksel `DELETE` mevcut; `Archived` durumu yumuşak (soft) alternatif sunar.
- **Kapsam dışı**: API Gateway, mesaj kuyruğu (asenkron iletişim), dağıtık izleme (tracing)
  — bu vaka için gerekli değil, üretimde önerilir.
