# Panduan Menambah Data dan Konten

PSEL menyimpan konten di dua tempat berbeda:

| Jenis data | Lokasi | Kapan dibaca | Perlu reset database? |
|---|---|---|---|
| Learning domain, module, material, objective, competency, prerequisite | `src/Modules/Psel.Modules.Learning/Seed/LearningSeedData.cs` atau import JSON di halaman **Authoring** | Saat API start, dan saat import, sebagai upsert berbasis slug | Tidak untuk tambah/ubah; ya untuk hapus/rename |
| Challenge package (manifest, statement, starter, tests, harness, solution) | `challenges/<domain>/<slug>/` atau upload ZIP di halaman **Authoring** | Saat API start, dan langsung setelah upload | Tidak, tidak perlu restart |
| Rubric dan examination variant | `challenges/<domain>/<slug>/challenge.json` | Saat API start atau upload | Tidak |
| Runtime, image Docker, dan limit eksekusi | `src/Psel.Api/appsettings.json` | Saat API start | Tidak, cukup restart API |
| Submission, assessment, rubric review | Dibuat lewat UI/API | Runtime | Tidak (data pengguna) |

Halaman **Authoring** (`http://localhost:5173/authoring`) menerima content pack JSON dan archive challenge. Kedua jalur memakai validasi server yang sama dengan seed dan startup, dan tetap mempertahankan submission, assessment, examination session, progress evidence, serta rubric review.

Template JSON referensi ada di [`docs/content-templates/`](content-templates/): skema + contoh learning content pack, manifest challenge coding/SQL, dan contoh test case. File tersebut adalah referensi format, bukan konten yang dibaca otomatis.

---

## 1. Menambah learning content

### 1.1 File yang diedit

Katalog learning bawaan didefinisikan di:

```text
src/Modules/Psel.Modules.Learning/Seed/LearningSeedData.cs
```

Untuk menambah konten tanpa mengubah kode, gunakan import JSON di bagian 1.7.

Struktur method yang tersedia:

```csharp
CreateDomain(slug, name, description, sortOrder, modules...)
CreateModule(slug, title, summary, level, estimatedMinutes, sortOrder,
             objectives[], competencies[(slug, name, description)], materials[])
CreateMaterial(slug, title, kind, estimatedMinutes, sortOrder, bodyMarkdown)
AddPrerequisite(module, prerequisiteModule)
```

`Level` yang dipakai UI: `Beginner`, `Intermediate`, `Advanced`. `Kind` material umumnya `Lesson`.

### 1.2 Menambah module ke path yang sudah ada

Tambahkan definisi module sebelum blok `return`, lalu masukkan ke `CreateDomain(...)` yang sesuai.

```csharp
var sortingAndSearching = CreateModule(
    "sorting-and-searching",
    "Sorting and Searching",
    "Choose a search strategy that matches the data and the constraints.",
    "Intermediate",
    50,
    4, // sortOrder di dalam path
    [
        "Compare linear and binary search trade-offs.",
        "Explain when sorting first is worth the extra cost."
    ],
    [
        (
            "search-strategy",
            "Search strategy",
            "Pick an approach based on ordering, input size, and update frequency."
        )
    ],
    [
        CreateMaterial(
            "sorting-and-searching-lesson",
            "Choosing a search strategy",
            "Lesson",
            20,
            1,
            """
            ## Choosing a search strategy

            Binary search is fast on sorted data, but sorting has a cost.
            Decide whether the data is already ordered before optimizing.
            """
        )
    ]);

AddPrerequisite(sortingAndSearching, algorithmFoundations);
```

Lalu tambahkan variabel itu ke domain:

```csharp
CreateDomain(
    "programming-fundamentals",
    "Programming Fundamentals",
    "...",
    1,
    algorithmFoundations,
    complexityAnalysis,
    dataStructures,
    sortingAndSearching),
```

### 1.3 Menambah learning path (domain) baru

Tambahkan satu entri `CreateDomain` baru dengan `sortOrder` berikutnya:

```csharp
CreateDomain(
    "security-engineering",
    "Security Engineering",
    "Threat modeling, secure defaults, and defensive coding.",
    6,
    threatModeling,
    secureCoding),
```

Path baru otomatis muncul di `/learning`, progress, dan form examination setelah API di-restart. Tidak ada perubahan navigation di frontend.

### 1.4 Aturan slug

- Gunakan huruf kecil, angka, dan tanda hubung: `sorting-and-searching`.
- `slug` domain, module, material, dan competency **unik secara global** (ada unique index di database).
- `prerequisite` di learning seed memakai **slug module**.
- `competencySlugs` di challenge harus memakai **slug competency** yang ada di seed learning. Inilah penghubung progress dan panel “Practice” di halaman learning.
- `prerequisites` di `challenge.json` juga memakai slug module, bukan slug challenge.

### 1.5 Ilustrasi lesson (opsional)

Setiap lesson bisa punya dua aset SVG di `client/public/illustrations/lessons/`:

| Aset | Nama file | Dipakai di |
|---|---|---|
| Cover | `<slug>-cover.svg` | Hero di atas judul lesson |
| Diagram konsep | `<slug>-diagram.svg` | Disisipkan di body Markdown |

Langkahnya:

1. Buat SVG dengan rasio cover `1200 × 420` dan diagram `960 × 480`. Keduanya panel gelap mandiri, jadi tetap terbaca di dark dan light theme.
2. Daftarkan slug lesson di `client/src/features/learning/lessonIllustrations.ts`. Lesson yang tidak terdaftar tetap tampil normal tanpa cover.
3. Sisipkan diagram di `bodyMarkdown` pada posisi yang relevan:

   ```markdown
   ![Diagram konsep untuk lesson ini](/illustrations/lessons/<slug>-diagram.svg)
   ```

   Teks `alt` menjadi deskripsi untuk pembaca layar. Judul diagram sebaiknya juga ditulis di dalam SVG.
4. Restart API agar body Markdown tersinkron, lalu buka halaman lesson di `http://localhost:5173`.

Cover memakai path publik Vite, jadi tidak perlu import atau rebuild backend. Menghapus satu aset cukup dengan menghapus entri slug dari `lessonIllustrations.ts` dan baris gambar di Markdown.

### 1.6 Cara seed bekerja (upsert)

`LearningDataSeeder` berjalan setiap API start dan memanggil `LearningSeedSynchronizer`. Perilakunya:

| Situasi | Yang terjadi |
|---|---|
| Slug belum ada di database | Baris baru ditambahkan |
| Slug sudah ada, field berubah | Field diskalakan (nama, deskripsi, judul, body Markdown, level, menit, sort order) diperbarui |
| Objective dengan `(ModuleId, SortOrder)` sama | Teks objective diperbarui |
| Material/competency dihapus manual dari database | Dibuat ulang saat restart |
| Prerequisite link hilang | Dibuat ulang saat restart |
| Baris dihapus dari seed code | **Tidak** dihapus dari database |
| Slug diganti (rename) | Baris lama tetap ada; baris baru ditambahkan |

Karena itu, menambah atau mengedit konten learning cukup dengan salah satu jalur:

1. Import content pack dari `/authoring` (bagian 1.7), atau
2. Edit `LearningSeedData.cs`, hentikan API, lalu jalankan lagi (`dotnet run --project src\Psel.Api`).

Untuk jalur seed code, cek log: `Synchronized 5 learning domains: N added, M updated.` Log hanya muncul bila ada perubahan.

Submission, assessment, examination session, progress evidence, dan rubric review tidak tersentuh karena baris yang sudah ada di-update, bukan dihapus lalu dibuat ulang.

#### Kapan reset database masih perlu?

Hanya bila kamu perlu **menghapus** konten yang sudah tidak ada di seed, atau mengganti slug secara sengaja. Upsert tidak menghapus baris yatim. Untuk kasus itu:

1. Hentikan API (Ctrl+C di terminal `dotnet run`, atau `Get-Process Psel.Api | Stop-Process`).
2. Backup database ke folder lain (jangan ke `data/` dengan nama `psel.db*`).
3. Hapus database beserta file WAL/SHM.
4. Jalankan API lagi; migrasi dan seed berjalan otomatis.

```powershell
# dari root repo
Get-Process Psel.Api -ErrorAction SilentlyContinue | Stop-Process

New-Item -ItemType Directory -Force -Path backup | Out-Null
Copy-Item src\Psel.Api\data\psel.db "backup\psel-$(Get-Date -Format yyyyMMdd-HHmmss).db"

Remove-Item src\Psel.Api\data\psel.db*

dotnet run --project src\Psel.Api
```

> **Peringatan:** reset database menghapus submission, assessment, examination session, progress evidence, dan rubric review. Backup dulu bila data itu masih kamu butuhkan.

### 1.7 Import content pack dari halaman Authoring

Cara tercepat menambah atau mengubah learning content tanpa rebuild:

1. Salin `docs/content-templates/learning-content.example.json`.
2. Isi `domains[].modules[]` beserta objectives, competencies, materials, dan prerequisites. `schemaVersion` harus `1`.
3. Buka `http://localhost:5173/authoring`, pilih file JSON, lalu klik **Import content**.
4. Panel sukses menampilkan jumlah domain, module, baris baru, dan baris yang diperbarui.

Lewat API, kirim JSON mentah:

```powershell
curl.exe -X POST http://localhost:5080/api/authoring/learning-content `
  -H "Content-Type: application/json" `
  --data-binary "@learning-pack.json"
```

Atau sebagai multipart file:

```powershell
curl.exe -X POST http://localhost:5080/api/authoring/learning-content `
  -F "file=@learning-pack.json;type=application/json"
```

Response sukses:

```json
{ "added": 3, "updated": 12, "domainCount": 1, "moduleCount": 2 }
```

Aturan import:

- `schemaVersion` harus `1`. Field yang tidak dikenal ditolak.
- Slug adalah identitas. Module, material, dan competency boleh diedit, tetapi tidak boleh dipindah ke domain/module lain lewat import.
- Objective memakai kunci `(module, sortOrder)`.
- `prerequisites` harus menunjuk module yang sudah ada di database atau ada di pack yang sama.
- Import tidak menghapus baris yang tidak ada di pack; gunakan reset database (bagian 1.6) untuk menghapus.
- Batas upload 2 MiB; `bodyMarkdown` maksimal 200.000 karakter per material.
- Semua validasi berjalan sebelum penulisan. Jika ada satu error, tidak ada baris yang berubah.

Validasi yang gagal mengembalikan `400` ProblemDetails berisi daftar error, `413` untuk file terlalu besar, dan `415` untuk content type yang tidak didukung.

### 1.8 Tes yang perlu diperbarui

Beberapa tes mengunci jumlah katalog. Setelah menambah learning data, sesuaikan:

| File | Yang diubah |
|---|---|
| `tests/Psel.Api.Tests/LearningSeedTests.cs` | Jumlah domain/module/material/competency/objective/prerequisite, serta nilai yang dipakai tes upsert |
| `tests/Psel.Api.Tests/LearningEndpointTests.cs` | Jumlah path dan module per path |
| `tests/Psel.Api.Tests/ProgressEndpointTests.cs` | Jumlah domain di `/api/progress` |
| `client/src/test/learningFixtures.ts` | Fixture UI bila menambah module/path yang dipakai tes frontend |

Tes ini sengaja gagal saat katalog berubah agar jumlah seed tidak bergeser tanpa sadar.

---

## 2. Menambah challenge

Panduan skema lengkap ada di [`challenges/README.md`](../challenges/README.md). Ringkasnya:

### 2.1 Struktur package

```text
challenges/<domain>/<slug>/
  challenge.json
  statement.md
  hints.md                  (opsional)
  starter/<language>/...
  tests/public/*.json
  tests/hidden/*.json
  harness/<language>/...    (C#/Python/Go; SQL memakai harness server)
  solutions/<language>/...  (server-only, tidak pernah dikirim ke client)
  sql/schema.sql            (khusus SQL)
  sql/seed.sql              (opsional, khusus SQL)
```

Discovery bersifat rekursif; nama folder tidak menentukan slug. `slug` di manifest yang authoritative.

### 2.2 Langkah menambah challenge

1. Salin package yang paling mirip, misalnya `challenges/programming/two-sum`.
2. Ubah `challenge.json`:
   - `slug`, `title`, `summary`, `domainSlug`, `topic`, `difficulty`, `type`, `estimatedMinutes`;
   - `prerequisites` (slug module learning);
   - `competencySlugs` (slug competency learning);
   - `statement`, `hints`, `tests`, `evaluation`.
3. Tulis `statement.md` dan (opsional) `hints.md`.
4. Sesuaikan starter files dan public/hidden tests.
5. Tambahkan harness hanya untuk `csharp`, `python`, atau `go`. Go memakai harness `package main` yang dibaca bersama file submission di dalam container `golang:1.27-alpine` (tanpa network, `GOPROXY=off`). SQL tidak boleh punya `harness`; server menyediakan harness dan memakai `sql.schema` + `sql.seed`.
6. Tambahkan reference solution di `solutions/` bila ada.
7. Restart API. Log startup harus menampilkan jumlah package yang berhasil dimuat:

```text
Loaded 25 challenge packages from D:\Aku\proyek\challenges.
```

Jika satu package tidak valid, API menolak start. Package yang gagal tidak pernah tampil sebagian.

### 2.3 Format test

Test standard (C#/Python/Go):

```json
{
  "input": {
    "numbers": [2, 7, 11, 15],
    "target": 9
  },
  "expected": [0, 1]
}
```

Test SQL dengan query-plan check:

```json
{
  "columns": ["id", "ordered_at", "total_cents"],
  "expected": [
    [2, "2026-01-05", 1500],
    [3, "2026-01-19", 1700]
  ],
  "plan": {
    "mustContain": ["USING INDEX idx_orders_customer_date"],
    "mustNotContain": ["SCAN orders"]
  }
}
```

- `tests.public` wajib untuk `AutomatedTests` dan `Hybrid`; nama test harus unik.
- Status dan nama test public boleh tampil ke client; isi `tests.hidden` tidak pernah dikirim.
- Test dengan `expectedStatus` dipakai untuk skenario error/exception.

### 2.4 Rubric (opsional)

Rubric hanya boleh saat `evaluation.kind` bernilai `Rubric` atau `Hybrid`.

```json
{
  "evaluation": {
    "kind": "Hybrid",
    "timeLimitSeconds": 5,
    "memoryLimitMb": 256
  },
  "rubric": {
    "criteria": [
      {
        "id": "behavior-preservation",
        "title": "Behavior preservation",
        "description": "Public behavior stays identical.",
        "maxPoints": 10,
        "weight": 3
      }
    ]
  }
}
```

- 1–12 kriteria, `id` unik (case-insensitive), `weight` default `1`.
- Client hanya mengirim poin dan catatan; server menghitung skor berbobot 0–100 dan band.
- Rubric bersifat advisory dan tidak mengubah assessment, status submission, atau progress.

### 2.5 Examination variant (opsional)

```json
{
  "examination": {
    "variants": [
      { "id": "standard", "label": "Standard examination", "timeLimitSeconds": 600 },
      { "id": "extended", "label": "Extended examination", "timeLimitSeconds": 900 }
    ]
  }
}
```

Tanpa blok ini, challenge tetap punya satu variant `standard` tanpa batas waktu. Server memilih variant; client tidak bisa meminta variant tertentu.

### 2.6 Setelah menambah challenge

- Restart API hanya diperlukan bila mengedit file package langsung. Upload lewat halaman Authoring langsung memuat ulang katalog.
- Cek `GET /api/challenges` dan buka `/challenges/<slug>` di UI.
- Jalankan public test dari workspace. Submit memerlukan Docker.
- Package tanpa harness tampil sebagai read-only (`canExecute: false`), tetap bisa dibaca tetapi tidak bisa dieksekusi.
- Tidak ada perubahan database. Tidak perlu reset.

### 2.7 Upload package dari halaman Authoring

1. Siapkan folder package valid (bagian 2.1), lalu zip isinya. `challenge.json` boleh berada di root ZIP atau di dalam satu folder.
2. Buka `/authoring`, pilih file `.zip`, lalu klik **Upload package**.
3. Jika slug sudah ada, centang **Replace the existing package with the same slug** sebelum upload.
4. Setelah sukses, challenge langsung tersedia di `GET /api/challenges` tanpa restart.

Aturan upload:

- Hanya `.zip`; maksimal 25 MiB, 1000 entry, 50 MiB setelah ekstrak, dan 5 MiB per file.
- ZIP harus berisi tepat satu `challenge.json`.
- Path absolut, `..`, symlink, dan entry duplikat ditolak.
- Package divalidasi dengan loader yang sama seperti startup di folder sementara. Jika validasi gagal, folder `challenges/` tidak berubah.
- Target install: `challenges/<domainSlug>/<slug>`.
- Respons tidak memuat path filesystem, hidden test, solution, atau harness.
- `409 Conflict` berarti package dengan slug tersebut sudah ada dan opsi replace belum diaktifkan.

---

## 3. Menyambungkan learning dan challenge

Hubungan terjadi lewat **slug competency**:

```text
LearningSeedData competencies[].slug
        ↕  harus sama
challenge.json competencySlugs[]
```

- Challenge hanya memberi evidence progress untuk competency yang dicantumkan.
- Halaman learning path dan module mencari irisan `challenge.competencySlugs` dengan competency module. Jika tidak ada irisan, panel “Practice” kosong.
- `domainSlug` challenge sebaiknya memakai slug learning domain yang ada, walau validasi loader hanya memeriksa format slug.
- `prerequisites` challenge memakai slug module learning (contoh `algorithm-foundations`).

Cek cepat mapping lewat API:

```powershell
Invoke-RestMethod http://localhost:5080/api/learning-paths |
  ForEach-Object { $_.slug; $_.modules | ForEach-Object { "  $($_.slug): $($_.competencySlugs -join ', ')" } }
```

---

## 4. Menambah data uji frontend (opsional)

Untuk tes UI, tambahkan fixture di:

- `client/src/test/learningFixtures.ts`
- `client/src/test/challengeFixtures.ts`
- `client/src/test/progressFixtures.ts`

Fixture bukan konten aplikasi; hanya dipakai Vitest dan tidak muncul di UI sebenarnya.

---

## 5. Verifikasi setelah menambah data

```powershell
# Backend
dotnet build PSEL.slnx
dotnet test tests\Psel.Api.Tests\Psel.Api.Tests.csproj

# Frontend
Set-Location client
npm run lint
npm run typecheck
npm run test:run
npm run build
Set-Location ..
```

Cek manual:

1. `http://localhost:5080/api/health` → `Healthy`.
2. `http://localhost:5080/api/learning-paths` → path/module baru muncul.
3. `http://localhost:5080/api/challenges` → challenge baru muncul.
4. `http://localhost:5173/learning` → kartu path menampilkan jumlah module dan challenge.
5. `http://localhost:5173/learning/<path>/<module>` → panel **Practice these competencies** terisi.
6. `http://localhost:5173/challenges/<slug>` → statement, starter, dan public test tampil.
7. `http://localhost:5173/authoring` → import JSON dan upload ZIP menampilkan panel sukses tanpa refresh.

---

## 6. Troubleshooting

| Gejala | Penyebab umum | Solusi |
|---|---|---|
| Learning content baru tidak muncul | Import belum dijalankan, slug/field salah, atau API belum di-restart setelah mengedit seed | Jalankan import dari `/authoring`, atau restart API dan baca log `Synchronized ...` |
| Konten hasil rename/hapus masih muncul | Upsert tidak menghapus baris yang hilang dari seed atau pack | Hapus manual baris terkait, atau reset database (bagian 1.6) |
| Import learning ditolak `400` | Ada field/slug ganda, parent dipindah, atau prerequisite tidak dikenal | Baca daftar error di respons; perbaiki pack lalu ulangi |
| API gagal start setelah menambah challenge | JSON tidak valid, field tidak dikenal, path keluar folder, atau test/harness tidak ada | Baca pesan validasi di log; perbaiki manifest |
| Upload challenge ditolak `409` | Slug sudah ada dan replace belum dicentang | Centang opsi replace, atau ubah slug |
| Upload challenge ditolak `400` | ZIP berisi lebih dari satu package, path tidak aman, atau manifest tidak valid | Baca detail error; pastikan hanya satu `challenge.json` |
| Challenge tampil tapi tidak bisa Run | `canExecute: false` karena harness tidak ada | Tambahkan harness yang valid, atau biarkan read-only |
| Panel Practice kosong | Slug competency challenge tidak cocok dengan seed | Samakan `competencySlugs` dengan slug di seed |
| “Port already in use” | API/Vite lama masih berjalan | Hentikan proses lama, atau jalankan dengan port lain |
| Submit gagal `ExecutionUnavailable` | Docker tidak berjalan | Start Docker Desktop; browsing tetap bisa tanpa Docker |

---

## 7. Yang belum didukung

- Editor in-app untuk isi package challenge; upload hanya menerima ZIP yang sudah lengkap.
- Penghapusan otomatis konten learning yang dihapus dari seed atau pack; gunakan reset atau cleanup manual.
- Penghapusan challenge lewat UI; hapus folder package secara manual.
- Autentikasi atau multi-user. Endpoint authoring hanya untuk mesin lokal tepercaya.
- Bahasa eksekusi baru (misalnya Java) lewat data saja; itu memerlukan adapter runner dan perubahan konfigurasi kode.
- Rubric-only challenge tanpa submission executable.
- Multi-user/auth; semua data bersifat lokal untuk satu pengguna.
