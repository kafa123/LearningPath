# Template JSON Konten

Folder ini berisi contoh dan skema JSON untuk menyiapkan konten. File di sini tetap berfungsi sebagai acuan format dan tidak dibaca otomatis oleh API. Learning content dan challenge sekarang bisa dipasang lewat halaman **Authoring** (`/authoring`) atau endpoint `/api/authoring/*`; jalur seed code dan folder `challenges/<domain>/<slug>/` tetap berlaku.

Gunakan file di sini sebagai acuan payload untuk import learning content dan upload challenge package.

## Learning content

| File | Isi |
|---|---|
| `learning-content.schema.json` | JSON Schema draft 2020-12 untuk satu content pack |
| `learning-content.example.json` | Contoh pack: satu domain dengan satu module baru |

Aturan yang dipakai contoh:

- `schemaVersion` harus `1`; field yang tidak dikenal ditolak.
- `slug` memakai huruf kecil, angka, dan tanda hubung.
- Domain, module, material, dan competency dicocokkan **berdasarkan slug**.
- Objective dicocokkan berdasarkan **`(module, sortOrder)`**.
- `prerequisites` berisi slug module lain, bukan slug challenge.
- Import bersifat upsert: menambah/mengubah aman, tetapi menghapus satu module dari JSON tidak menghapusnya dari database.
- Module, material, dan competency tidak boleh dipindah ke parent lain lewat import.
- `learning-content.example.json` menambahkan module `sorting-and-searching` ke domain `programming-fundamentals`. Module lain di domain tersebut tidak perlu ditulis ulang.

Cara import: buka `/authoring`, unggah file JSON, lalu klik **Import content**. Atau kirim langsung ke API:

```powershell
curl.exe -X POST http://localhost:5080/api/authoring/learning-content `
  -F "file=@docs/content-templates/learning-content.example.json;type=application/json"
```

Validasi cepat:

```powershell
# PowerShell 7+: cek sintaks sekaligus schema
Get-Content docs\content-templates\learning-content.example.json -Raw |
  Test-Json -SchemaFile docs\content-templates\learning-content.schema.json

# PowerShell 5.1: cek sintaks JSON saja
Get-Content docs\content-templates\learning-content.example.json -Raw |
  ConvertFrom-Json
```

Jika Python dan paket `jsonschema` tersedia:

```powershell
python -c "import jsonschema,json,pathlib; s=json.loads(pathlib.Path('docs/content-templates/learning-content.schema.json').read_text(encoding='utf-8')); d=json.loads(pathlib.Path('docs/content-templates/learning-content.example.json').read_text(encoding='utf-8')); jsonschema.validate(d,s); print('OK')"
```

## Challenge package

| File | Isi |
|---|---|
| `challenge-manifest.example.json` | Manifest challenge coding C# + Python dengan automated tests |
| `challenge-manifest-sql.example.json` | Manifest challenge SQL dengan schema, seed, dan query-plan test |
| `test-case.example.json` | Contoh file test standard (`input` + `expected`) |
| `test-case-sql.example.json` | Contoh file test SQL (`columns`, `expected`, `ordered`, `plan`) |

Manifest challenge harus disimpan sebagai `challenge.json` di dalam package:

```text
challenges/<domain>/<slug>/
  challenge.json
  statement.md
  hints.md
  starter/<language>/...
  tests/public/*.json
  tests/hidden/*.json
  harness/<language>/...     (C#/Python; SQL tidak boleh punya harness)
  solutions/<language>/...   (server-only)
  sql/schema.sql             (khusus SQL)
  sql/seed.sql               (opsional, khusus SQL)
```

Catatan:

- Semua path di manifest relatif terhadap folder package.
- File yang direferensikan harus ada; package yang tidak valid membuat API menolak start atau menolak upload.
- `tests.public` dan `tests.hidden` wajib untuk `AutomatedTests` dan `Hybrid`.
- Isi `tests.hidden`, `harness`, `solutions`, dan SQL schema/seed tidak pernah dikirim ke browser.
- Untuk SQL, entry `languages` memakai `id: "sql"` tanpa `harness`.
- Manifest contoh mereferensikan `statement.md`, `hints.md`, starter, harness, dan test. Buat file tersebut sebelum menyalin manifest ke folder `challenges/` atau memasukkannya ke ZIP.

Upload: zip isi folder package sehingga `challenge.json` berada di root ZIP atau di dalam satu folder, lalu unggah dari `/authoring`. ZIP harus berisi tepat satu package; centang opsi replace bila slug sudah ada.

Panduan lengkap: [`docs/ADDING_CONTENT.md`](../ADDING_CONTENT.md).
