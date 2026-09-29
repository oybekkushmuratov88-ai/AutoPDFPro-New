# Registration

Optional feature that lets the **author** know who uses the plug-in. It is **off by default** and turns on
only when the author deploys a small Google Apps Script and puts its URL in the plug-in. Everything is done
with the user's knowledge and consent; no password or secret is ever shipped inside the plug-in.

## What the user sees

Registration happens **during installation** (Setup.exe): after the language dialog and the welcome page,
a page collects **name**, **e-mail** (and an optional phone) with a **consent** tick
(*"I agree to send the data for processing"*). Setup sends it to the web app and writes a machine-wide marker
(`HKLM\Software\AutoPDFPro`), so once installed **AUTOPDF works straight away** — no window on first run.

If the computer has no internet during install, the install still completes and the plug-in re-sends the
registration automatically on a later `AUTOPDF` run (via the .NET client), so the author still receives it.

For a manual install (the zip, without Setup.exe) the plug-in falls back to asking on the first `AUTOPDF`:
the same window, `RegistrationConfig.Mandatory = true`, with an offline "send later and continue" option.

## What the author gets

* an **e-mail** for every new user: *"… <name> registered … from the computer <computer>."*;
* a **Google Sheet** row per user (date, name, e-mail, phone, computer, Windows user, machine id, OS,
  version, language);
* the same list inside AutoCAD via **`AUTOPDFADMIN`** + the admin key.

## How it stays safe

| Concern | How it is handled |
|---|---|
| The author's Gmail password | Never in the plug-in. The e-mail is sent by `MailApp` **inside the author's Google account** (the Apps Script runs as the author). The plug-in only holds the public web-app URL. |
| The user's Google password | Never requested. The user types their own e-mail and consents; there is no "sign in with Google" password prompt. |
| Who can see the user list | Protected by an **admin key** that the author invents and the server checks. The key is typed by the author (optionally remembered on their own machine in `%APPDATA%\AutoPDFPro\admin.txt`); it is not in the distributed plug-in. |
| Restricted-internet workstations | Not locked out: the registration is stored locally and re-sent when the machine is next online. |
| Turning it off | Leave `RegistrationConfig.Endpoint` as the placeholder — the whole feature is disabled and the plug-in behaves exactly as before. |

## Data flow

```
Setup.exe ── registration page (name, e-mail, consent) ──▶ PowerShell POST to the web app
     │                                                              │
     └─ writes HKLM\Software\AutoPDFPro (Registered=1, …)           ▼
                    │                            Google Apps Script (Code.gs, runs as the author)
                    ▼                                     ├─ MailApp.sendEmail → author's Gmail
AUTOPDF ─▶ EnsureRegistered()                            └─ Sheet.appendRow   → Google Sheet
             │  HKLM marker present? ─ yes ─▶ run (re-send via .NET client only if not delivered)
             │  no → %APPDATA% registration.xml? ─ yes ─▶ run
             │  no → RegistrationWindow (manual-install fallback) ─▶ HttpRegistrationGateway.Submit

AUTOPDFADMIN ─▶ UsersWindow ── admin key ──▶ FetchUsers (GET ?action=list&key=…)
                                                    ▼
                                   Apps Script checks the key → OK + tab-separated rows
```

## Files

| File | Role |
|---|---|
| `src/AutoPDF.Core/Registration/RegistrationConfig.cs` | The web-app URL, the mandatory/offline flags, the admin e-mail. **Set `Endpoint` here.** |
| `src/AutoPDF.Core/Registration/RegistrationInfo.cs`, `RegistrationStore.cs` | The stored registration and its XML file. |
| `src/AutoPDF.Core/Registration/MachineFacts.cs` | Computer / Windows user / OS / stable machine id. |
| `src/AutoPDF.Core/Registration/RegistrationGateway.cs` | The HTTP client (form POST + TSV GET). Unit-tested against a local `HttpListener`. |
| `src/AutoPDF.UI/Registration/RegistrationWindow.cs` (+VM) | The first-run window. |
| `src/AutoPDF.UI/Registration/UsersWindow.cs` (+VM) | The admin user list. |
| `installer/registration/Code.gs` | The Apps Script the author deploys. |
| `installer/registration/README.md` | Step-by-step deployment (≈ 5 min). |

## Switching it on

1. Deploy `installer/registration/Code.gs` as a Google Apps Script web app — see
   [installer/registration/README.md](../installer/registration/README.md). Note the `/exec` URL and the
   `ADMIN_KEY` you chose.
2. Put the `/exec` URL in `RegistrationConfig.Endpoint` and rebuild (`build/build.ps1` or `build/build.sh`).
   `IsConfigured` becomes true and registration is enforced from then on.
3. Preview the windows without AutoCAD: `AutoPDF.UiDemo.exe --demo register` and `--demo users`.
