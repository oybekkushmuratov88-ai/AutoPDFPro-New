# Registration and administrator approval

AutoPDF Pro creates PDFs only on installations the administrator has approved. The back end is a Google
Apps Script bound to a Google Sheet ([server/licensing/Code.gs](../server/licensing/Code.gs)); deployment
steps are in [server/licensing/README.md](../server/licensing/README.md).

## Flow

```
first AUTOPDF ─▶ registration form (name, e-mail, installation ID, consent)
                    │ HTTPS POST action=register
                    ▼
              Apps Script ── new row "pending" in the Requests sheet
                    │      └─ e-mail to the administrator: Approve / Reject (signed, expiring links)
                    ▼
plug-in ◀── status=pending ──  "Pending approval" window (re-checks every minute; PDF export disabled)

administrator ─▶ link → confirmation page → button     ┐
             or ─▶ sheet menu AutoPDF ▸ Approve/Reject  ├─▶ status approved / rejected (+ who/when)
             or ─▶ edit the Status column               ┘    └─ e-mail to the user

plug-in ─▶ HTTPS POST action=status ◀── status=approved + licence token + RSA signature
             verifies the signature, installation ID, computer and expiry → AUTOPDF works
             re-checks on every AUTOPDF (background) and every few minutes while the CAD runs
```

## States (client)

| State | Meaning | AUTOPDF |
|---|---|---|
| NotRegistered | nothing accepted by the server yet | registration form |
| Pending | registered, waiting for the administrator | "Pending approval" window |
| Approved | a valid signed approval for this installation and computer, not expired | runs |
| Rejected | rejected, or approval revoked later | "Not approved" window |
| Expired | approved, but offline longer than the grace period, or the clock was turned back | needs an online check |
| Invalid | the stored approval does not verify (edited file, other computer, other key) | needs an online check |

## Protocol

HTTPS `POST` to the web app (`…/exec`), form-encoded; the reply is `text/plain`, one `key=value` per line
(values URL-encoded), first line `apdf=1`.

Request (all actions): `v=1`, `action` (`register` | `status`), `nonce` (32 hex, unique per request),
`installId` (`APDF-XXXX-XXXX-XXXX-XXXX`, 80 random bits), `clientKey` (256 random bits, base64url),
`machine` (SHA-256 of the Windows MachineGuid + user), `app`, `host`, `lang`.
`register` adds `name`, `email`, `computer`, `winUser`, `os`, `consent=1`.

Reply: `ok` (1/0), `status` (`pending` | `approved` | `rejected`), `installId`, `requestId` (UUID, one per
registration), `serverTime`, `publicKey` + `keyId`, and for approved: `token` + `sig`; on errors `error`
(`invalid_field:<name>`, `rate_limited`, `replay`, `unknown_install`, `client_mismatch`, `machine_mismatch`,
`server_error`) and optionally `checkAfter` (seconds).

Licence token (signed with RSASSA-PKCS1-v1_5 / SHA-256):

```
AUTOPDF-LIC|1|<installId>|<machine>|approved|<requestId>|<issuedAt>|<expiresAt>      (Unix seconds)
```

## Security properties

| Requirement | How |
|---|---|
| No administrator secret in the client | The plug-in holds the public web-app URL and (at most) the **public** key. The RSA private key and the link secret exist only in the script's *Script Properties* in the administrator's Google account. |
| HTTPS | The client refuses non-`https` endpoints (loopback test servers excepted); certificates are validated by .NET. Google requires TLS 1.2+. |
| Unique request IDs | A server-generated UUID per registration (row key, used in links and tokens) and a client nonce per request; a repeated nonce is refused (`replay`). |
| Authenticated approvals | E-mail links carry `HMAC-SHA256(link secret, "review|requestId|op|expiry")` and expire after 7 days; opening one only shows a confirmation page (mail scanners cannot approve by visiting); the button calls the server, which re-checks the signature. The sheet menu and the Status column are protected by Google sign-in (the sheet's owner/editors). Every web-callable admin function checks that the caller is the script owner. |
| Signed licences | The client accepts "approved" only with a valid signature over a token that names *this* installation ID, request ID and computer and has not expired. Editing `license.txt` or copying it to another computer cannot create or move a licence. |
| Server-side validation | Formats of every field (installation ID, client key, machine hash, nonce, e-mail, lengths), consent required, control characters removed, text that a spreadsheet would treat as a formula stored as plain text. |
| Ownership of an installation | The server stores only SHA-256 of the client key; status requests must present the key (`client_mismatch` otherwise) and come from the same computer (`machine_mismatch`), so knowing an installation ID is not enough. |
| Throttling | Server: 5 registrations/hour per installation and per e-mail address, 60 new requests/hour overall (protects the administrator's mailbox and the MailApp quota), 120 status checks/hour per installation. Client: ≥ 20 s between checks while pending (5 min in the background), 30 min after an approval (55 min in the background), "check after" from the server is honoured. |
| Internet failures | Never grant anything new: an unsent registration stays unregistered, a pending request stays pending. An approval keeps working offline only until its signed expiry (14 days by default; `CFG.licenseTtlDays`). A clock turned back by more than an hour distrusts the cached approval until the server is reached. |
| Revocation | Setting a request back to *rejected* takes effect at the next online check (≤ 30 min of use, or at once on the next AUTOPDF after a background check). |

### Where the client gets the public key

* Default: the server sends its public key with every reply over HTTPS; the client stores it with the licence
  (the configured HTTPS endpoint is the trust anchor). Rotating the key on the server is seamless.
* Strict mode: put the key from *AutoPDF ▸ Show public key* into `LicenseConfig.PinnedPublicKey`
  (`src/AutoPDF.Core/Licensing/LicenseConfig.cs`) and rebuild. Then only that key is accepted, so even a
  locally replaced key file cannot validate a forged token; a key rotation then needs a new build.

Like any client-side check, the plug-in can be bypassed by someone who modifies its binaries on their own
computer; the system guarantees that approvals come only from the administrator and cannot be forged or
transferred by editing files or replaying requests.

## Files

| File | Role |
|---|---|
| `src/AutoPDF.Core/Licensing/LicenseConfig.cs` | Web-app URL, optional pinned public key |
| `src/AutoPDF.Core/Licensing/LicenseManager.cs` | State machine, offline rules, throttling |
| `src/AutoPDF.Core/Licensing/LicenseServer.cs` | HTTPS client and reply parser |
| `src/AutoPDF.Core/Licensing/LicenseToken.cs`, `RsaPublicKey.cs` | Token format, signature verification (DER parser for .NET 4.8) |
| `src/AutoPDF.Core/Licensing/InstallationIdentity.cs` | Installation ID, client key, computer binding |
| `src/AutoPDF.Core/Licensing/LicenseStore.cs` | `%LOCALAPPDATA%\AutoPDFPro\license.txt` |
| `src/AutoPDF.UI/Licensing/*` | Registration form, status window, notification |
| `src/AutoPDF.Cad/Licensing/LicenseGate.cs` | AUTOPDF gate, background/periodic checks, notifications |
| `server/licensing/Code.gs` | The back end |
| `server/licensing/test/*` | Node tests with Google service mocks; `serve.js` for the C# end-to-end test |
