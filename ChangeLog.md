# SFMail

SFMail provides the ability to send emails from VFP applications, including support for Modern Authentication.

## Release history

## 2025-07-29: version 2025.07.29

* Attachment file names are now encoded as UTF-8 (thanks to kobruleht for the suggestion).

* GetToken in SFMail.prg now uses DATETIME() rather than SECONDS() to avoid issues with a date boundary.

## 2025-04-05

* SMTPLibrary2.dll (the Product Version property) and SFMail (the cVersion property) now have version numbers: 2025.04.05.

* The MailKit and MimeKit libraries were upgraded to the latest versions due to bug fixes and security issues with older libraries. As a result, the DLLs to deploy have changed: BouncyCastle.Crypto.dll, MailKit.dll, and MimeKit.dll are no longer needed but the following DLLs must be deployed:

    * MailKitLite.dll
    * MimeKitLite.dll
    * System.Buffers.dll
    * System.Formats.Asn1.dll
    * System.Memory.dll
    * System.Numerics.Vectors.dll
    * System.Runtime.CompilerServices.Unsafe.dll
    * System.Threading.Tasks.Extensions.dll
    * System.ValueTuple.dll

### 2022-09-17

* The new lUseHTML property allows you to control whether the email is sent as HTML or plain text.

### 2022-09-10

* No longer gives project manager build errors when VFPExMAPI.fll is not present.

### 2022-03-16

* Added support for Modern Authentication.

### 2021-02-04

* Initial release.
