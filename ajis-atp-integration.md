## Implementation note for .NET (locked)

* The **ATP index (entry table)** is **binary**.
* AJIS payload elements reference attachments via an ID/name and may include metadata (content type, file name, etc.).
* Attachments are stored as contiguous blobs.
* The reader resolves an attachment by using the entry table to obtain **offset + size**.
* Integrity MAY be validated using the entry checksum (e.g. CRC32) at read time.

This separation keeps AJIS semantic and developer-facing, while ATP remains transport-level and allocation-free.
