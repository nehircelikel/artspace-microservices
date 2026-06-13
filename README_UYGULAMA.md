# ArtSpace — Commission Service + Like/Save Eklentisi

Bu zip, ArtSpace projesine **Commission Service** (yeni mikroservis) ve **Like/Save**
(Comment Service içine) özelliklerini ekler. Dosyalar repo içindeki yollarıyla
düzenlendi; bu zip'in içeriğini repo kök klasörünün ÜSTÜNE açman yeterli (var olan
dosyalar güncellenir, yeni dosyalar eklenir).

## 1. Bu zip'teki dosyalar

### YENİ — CommissionService/ (komple yeni mikroservis)
- Core: Commission entity + ICommissionRepository
- Infrastructure: DbContext, CommissionRepository, RabbitMQPublisher
- API: CommissionController, DTOs, Program.cs, appsettings.json
- Dockerfile, .sln

### DEĞİŞEN — mevcut dosyalar (üzerine yazılır)
- docker-compose.yml          -> commission-service + commissiondb + gateway depends_on eklendi
- ApiGateway/.../ocelot.json   -> /api/Commission ve /api/Like route'ları eklendi
- NotificationService/.../RabbitMQConsumer.cs -> commission_created kuyruğu da dinleniyor
- CommentService/...           -> Like/Save dosyaları (entity, repo, controller, DTO, DbContext, Program.cs)

## 2. Uygulama adımları (kendi makinende, repo kökünde)

### a) Zip'i aç
Zip içeriğini repo kök klasörüne (artspace-microservices/) açıp var olan dosyaların
üzerine yazılmasına izin ver.

### b) Migration oluştur
Commission (yeni DB tablosu):
    cd CommissionService
    dotnet ef migrations add InitialCreate --project CommissionService.Infrastructure --startup-project CommissionService.API
    cd ..

Like (Comment Service'e yeni tablo):
    cd CommentService
    dotnet ef migrations add AddLikes --project CommentService.Infrastructure --startup-project CommentService.API
    cd ..

NOT: Program.cs dosyaları zaten db.Database.Migrate() çağırıyor, yani uygulama
ayağa kalkınca tablolar otomatik oluşur. Migration dosyalarını yine de bir kez
oluşturman gerekiyor (yukarıdaki komutlar).

### c) Çalıştır
    docker-compose up --build

Tüm servisler + commissiondb + RabbitMQ birlikte ayağa kalkar.

## 3. Test (Postman / Swagger — gateway üzerinden)

Gateway: http://localhost:5092

Önce login olup JWT token al (Auth servisi). Sonra Authorization: Bearer <token>.

Commission:
- POST  /api/Commission                 (talep oluştur; body: artistId, title, description, budget)
- GET   /api/Commission/received         (sana gelen talepler)
- GET   /api/Commission/sent             (gönderdiğin talepler)
- PUT   /api/Commission/{id}/status      (sanatçı: Accepted/Rejected/Completed)

Like/Save:
- POST  /api/Like/artwork/{artworkId}        (beğen/kaldır - toggle)
- GET   /api/Like/artwork/{artworkId}/count  (beğeni sayısı - public)
- GET   /api/Like/artwork/{artworkId}/status (kullanıcı beğenmiş mi)
- GET   /api/Like/my                          (kullanıcının kaydettikleri)

## 4. Doğrulama
- Bir komisyon talebi oluştur -> sanatçının /api/Notification listesinde bildirim çıkmalı
  (RabbitMQ commission_created -> Notification Service akışı).
- RabbitMQ management UI: http://localhost:15672 (guest/guest) -> commission_created kuyruğu görünür.

## Portlar
- Auth 5144 | Art 5280 | Comment 5285 | Notification 5012 | Commission 5013 | Gateway 5092
- Frontend 3000 | Postgres 5432 | RabbitMQ 5672 (UI 15672)
