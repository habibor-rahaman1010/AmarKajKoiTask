# Amar Kaj Koi (আমার কাজ কই)

Textile / RMG factory-র জন্য **voice + form based task management system**।
Top Management ভয়েসে Target দেয়, Employee ফর্মে Commitment দেয়, Voice Reviewer সেই ভয়েসকে structured task-এ রূপান্তর করে — পুরো lifecycle timeline সহ ট্র্যাক হয়।

---

## 🧱 Tech Stack

| Layer | Technology |
|---|---|
| Backend API | ASP.NET Core Web API (**.NET 9**) |
| Data Access | Dapper (Raw SQL, Unit of Work pattern) |
| Database | **SQL Server** |
| Auth | JWT Bearer Token |
| Realtime | SignalR (`/hubs/notifications`) |
| Logging | Serilog (file sink → `Logs/`) |
| Frontend | **Angular 20** + PrimeNG + TailwindCSS + SweetAlert2 |

---

## 📁 Project Structure

```
MultiTechSystem/
├── README.md
├── srs_amar_kaj_koi.txt              # Software Requirements Specification
└── src/
    ├── AmarKajKoi/                   # Backend (ASP.NET Core Web API)
    │   ├── AmarKajKoi.sln
    │   └── AmarKajKoi/
    │       ├── Controllers/          # Auth, Tasks, Voice, Notifications, ReferenceData
    │       ├── Database/
    │       │   └── Scripts/
    │       │       ├── 01_Schema.sql     # ⭐ Database + সব টেবিল তৈরি
    │       │       └── 02_SeedData.sql   # ⭐ Roles, Statuses, Default Users
    │       ├── Entities/ DataTransferObjects/
    │       ├── RepositoriesImplement/ ServicesImplement/
    │       ├── Hubs/                 # SignalR NotificationHub
    │       ├── Middleware/           # Global error handling
    │       ├── wwwroot/UploadedVoiceFiles/   # ভয়েস ফাইল এখানে জমা হয়
    │       ├── appsettings.json      # ⭐ Connection String + JWT config
    │       └── Program.cs
    │
    └── amar-kaj-koi-web/             # Frontend (Angular 20)
        ├── src/app/pages/            # সব screen
        ├── src/app/core/             # auth guard, interceptor, services
        ├── src/environments/         # ⭐ API URL এখানে সেট করা
        └── package.json
```

---

## ✅ Prerequisites (আগে যা যা ইনস্টল থাকতে হবে)

প্রজেক্ট চালানোর আগে নিচের সফটওয়্যারগুলো ইনস্টল করে নিন:

| Software | Version | Download Link |
|---|---|---|
| **.NET SDK** | 9.0 বা তার উপরে | https://dotnet.microsoft.com/download/dotnet/9.0 |
| **SQL Server** | 2019 বা তার উপরে (Express চলবে) | https://www.microsoft.com/sql-server/sql-server-downloads |
| **SSMS** | Latest | https://aka.ms/ssmsfullsetup |
| **Node.js** | 20.x বা তার উপরে (LTS) | https://nodejs.org |
| **Angular CLI** | 20.x | `npm install -g @angular/cli` |
| **Visual Studio 2022** (optional) | 17.12+ (.NET 9 workload সহ) | https://visualstudio.microsoft.com |

ইনস্টল ঠিক আছে কিনা চেক করুন:

```bash
dotnet --version     # 9.x.x আসতে হবে
node --version       # v20.x বা উপরে
ng version           # Angular CLI 20.x
```

---

## 🚀 Setup & Run (ধাপে ধাপে)

### ধাপ ১ — Clone করুন

```bash
git clone https://github.com/habibor-rahaman1010/AmarKajKoiTask/tree/developer
cd AmarKajKoiTask
```

---

### ধাপ ২ — Database তৈরি করুন ⚠️ (সবচেয়ে গুরুত্বপূর্ণ ধাপ)

Database automatic তৈরি হয় না — **SQL script দুইটা নিজে হাতে চালাতে হবে**।

1. **SSMS** খুলে আপনার SQL Server instance-এ কানেক্ট করুন।
2. এই ফাইলটা খুলে **Execute (F5)** করুন:
   ```
   src/AmarKajKoi/AmarKajKoi/Database/Scripts/01_Schema.sql
   ```
   > এটা `AmarKajKoiDB` নামে database তৈরি করবে এবং সব টেবিল বানাবে।
   >
   > ⚠️ **সতর্কতা:** স্ক্রিপ্টের শুরুতেই `DROP DATABASE` আছে — অর্থাৎ `AmarKajKoiDB` আগে থেকে থাকলে সেটা **মুছে গিয়ে নতুন করে তৈরি হবে**। ডাটা থাকলে আগে ব্যাকআপ নিন।

3. এরপর এই ফাইলটা **Execute (F5)** করুন:
   ```
   src/AmarKajKoi/AmarKajKoi/Database/Scripts/02_SeedData.sql
   ```
   > এটা Roles, Task Statuses, Task Centers, Event Channels, Day Events এবং **৪টা ডিফল্ট ইউজার** তৈরি করবে।

**ক্রম অবশ্যই এই রকম হতে হবে: `01_Schema.sql` → তারপর `02_SeedData.sql`**

---

### ধাপ ৩ — Connection String ঠিক করুন ⚠️

ফাইল: `src/AmarKajKoi/AmarKajKoi/appsettings.json`

```json
"ConnectionStrings": {
  "Default": "Server=.\\SQL19;Database=AmarKajKoiDB;User Id=habibor144369;Password=c++c++c#;TrustServerCertificate=True;MultipleActiveResultSets=True"
}
```

এখানে `Server`, `User Id`, `Password` — **আপনার নিজের SQL Server-এর তথ্য দিয়ে বদলে নিন**।

**Windows Authentication ব্যবহার করলে:**

```json
"Default": "Server=.\\SQLEXPRESS;Database=AmarKajKoiDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
```

সাধারণ `Server` ভ্যালুগুলো: `.` | `localhost` | `.\SQLEXPRESS` | `.\SQL19` | `DESKTOP-XXXX\SQLEXPRESS`

---

### ধাপ ৪ — Backend চালু করুন

```bash
cd src/AmarKajKoi/AmarKajKoi
dotnet restore
dotnet run
```

Backend চালু হবে: **http://localhost:5080**
Swagger UI: **http://localhost:5080/swagger**

> Visual Studio ব্যবহার করলে: `src/AmarKajKoi/AmarKajKoi.sln` ওপেন করে **F5** চাপুন।

---

### ধাপ ৫ — Frontend চালু করুন (নতুন টার্মিনালে)

```bash
cd src/amar-kaj-koi-web
npm install
npm start
```

Frontend চালু হবে: **http://localhost:4200**

ব্রাউজারে http://localhost:4200 খুললে লগইন পেজ আসবে।

> ⚠️ **Backend আর Frontend একসাথে চালু থাকতে হবে।** দুইটা আলাদা টার্মিনাল লাগবে।

---

## 🔑 Login Credentials (ডিফল্ট ইউজার ও পাসওয়ার্ড)

`02_SeedData.sql` চালানোর পর নিচের ৪টা অ্যাকাউন্ট রেডি থাকবে।
লগইনের সময় **Username** লাগবে (Email দিয়ে লগইন হবে না)।

| # | Role | Username | Password | কী করতে পারবে |
|---|---|---|---|---|
| 1 | **Top Management** | `manager` | `Manager@123` | ভয়েসে Target তৈরি, Commitment Approve/Reject, Extend request Approve/Reject, Due date পরিবর্তন, Passed/Failed/Cancelled মার্ক, Pin, সব Dashboard |
| 2 | **Employee** | `employee` | `Employee@123` | ফর্মে Commitment তৈরি, Extend/Revise রিকোয়েস্ট (সর্বোচ্চ ৩ বার), Mark as Passed রিকোয়েস্ট, নিজের Performance |
| 3 | **Voice Reviewer** | `reviewer` | `Reviewer@123` | Management-এর ভয়েস Target শুনে structured task-এ রূপান্তর, Send Back for Correction |
| 4 | **System Admin** | `admin` | `Admin@123` | টেকনিক্যাল রোল — Failed/Cancelled টাস্ক আবার Reopen, User ম্যানেজমেন্ট |

📧 Email গুলো (রেফারেন্সের জন্য, লগইনে লাগবে না):
`manager@amarkajkoi.local` · `employee@amarkajkoi.local` · `reviewer@amarkajkoi.local` · `admin@amarkajkoi.local`

> 🔐 **কিভাবে কাজ করে:** seed করা পাসওয়ার্ড ডাটাবেজে `PLAIN:` প্রিফিক্স দিয়ে রাখা হয়। প্রথমবার সফল লগইনের পর সেটা PBKDF2 hash-এ আপগ্রেড হয়ে যায়। তাই ডাটাবেজে সরাসরি টেবিল দেখে পাসওয়ার্ড পড়া যাবে না।
>
> ⚠️ এগুলো **শুধু ডেভেলপমেন্ট/ডেমোর জন্য**। Production-এ যাওয়ার আগে অবশ্যই পাসওয়ার্ড বদলাবেন।

---

## 🧭 নতুন ইউজার তৈরি করবেন কিভাবে

`manager` অথবা `admin` দিয়ে লগইন করে **Admin → Users** (`/admin/users`) পেজে যান, সেখান থেকে নতুন ইউজার তৈরি করা যাবে।

অথবা Swagger থেকে:
1. `POST /api/auth/login` → `manager` / `Manager@123` দিয়ে token নিন
2. Swagger-এর **Authorize** বাটনে token পেস্ট করুন (`Bearer ` লিখবেন না, শুধু raw token)
3. `POST /api/auth/register` কল করুন:

```json
{
  "fullName": "Karim Ahmed",
  "username": "karim",
  "email": "karim@amarkajkoi.local",
  "password": "Karim@123",
  "roleName": "Employee"
}
```

`roleName` হতে পারে: `TopManagement` | `Employee` | `VoiceReviewer` | `SystemAdmin`
(একজন ইউজারের একটাই রোল থাকবে)

---

## 🗺️ Page / Screen List

| Route | Screen | কোন রোল দেখতে পাবে |
|---|---|---|
| `/login` | Login | সবাই |
| `/dashboard` | Dashboard | সবাই (রোল অনুযায়ী আলাদা) |
| `/tasks` | Task List | সবাই |
| `/tasks/:id` | Task Detail + Timeline | সবাই |
| `/tasks/new-target` | New Target (Voice) | Top Management |
| `/tasks/new-commitment` | New Commitment (Form) | Employee |
| `/voice-review` | Voice-to-Task Review | Voice Reviewer |
| `/approvals` | Approve / Reject Commitment | Top Management |
| `/extend-requests` | Extend / Revise Requests | Top Management |
| `/final-tasks` | Passed / Failed / Cancelled | Top Management |
| `/my-commitments` | My Commitments | Employee |
| `/performance` | নিজের Performance | Employee |
| `/performance-all` | সবার Performance | Top Management |
| `/event-tasks` | Event-wise Task List | সবাই |
| `/notifications` | Notifications | সবাই |
| `/admin/users` | User Management | Top Management, System Admin |

---

## 🔄 কাজের ফ্লো একবার টেস্ট করে দেখুন

**ফ্লো ১ — Management Target (Voice):**
1. `manager` দিয়ে লগইন → **New Target** → ভয়েস রেকর্ড (কমপক্ষে ৩ সেকেন্ড) → **Post**
   → স্ট্যাটাস হবে `Pending Voice Review`
2. লগআউট করে `reviewer` দিয়ে লগইন → **Voice Review** → অডিও শুনে ফর্ম পূরণ (Task Center, Event Channel, Task Name, Due Date, Assigned To) → **Save & Open Task**
   → স্ট্যাটাস হবে `Open`, Assigned Employee নোটিফিকেশন পাবে
3. `employee` দিয়ে লগইন → টাস্ক দেখা যাবে, Extend রিকোয়েস্ট বা Mark as Passed রিকোয়েস্ট করা যাবে

**ফ্লো ২ — Employee Commitment (Form):**
1. `employee` দিয়ে লগইন → **New Commitment** → ফর্ম পূরণ → **Post**
   → স্ট্যাটাস `Pending Management Approval`
2. `manager` দিয়ে লগইন → **Approvals** → Approve
   → স্ট্যাটাস `Open`

> 🎤 ভয়েস রেকর্ডিংয়ের জন্য ব্রাউজারে **মাইক্রোফোন পারমিশন** দিতে হবে। Chrome-এ `localhost` এ HTTP দিয়েও মাইক কাজ করে।

---

## ⚙️ Configuration Reference

| কী বদলাতে চান | কোন ফাইলে |
|---|---|
| Database connection string | `src/AmarKajKoi/AmarKajKoi/appsettings.json` |
| JWT secret / token মেয়াদ (default ৪৮০ মিনিট) | `appsettings.json` → `Jwt` |
| ভয়েস ফাইল কতদিন রাখবে (default ৩৬৫ দিন) | `appsettings.json` → `VoiceRetention:Days` |
| Backend port (default 5080) | `Properties/launchSettings.json` |
| Frontend যে API URL-এ কল করে | `src/amar-kaj-koi-web/src/environments/environment.ts` |
| CORS-এ কোন origin allowed | `Extensions/ServiceCollectionExtensions.cs` → `AddAppCors()` |

Backend-এর port বদলালে **দুই জায়গায়** বদলাতে হবে:
- `environment.ts` → `apiUrl` ও `hubUrl`
- `ServiceCollectionExtensions.cs` → CORS origin (frontend-এর port বদলালে)

---

## 🛠️ Troubleshooting

| সমস্যা | সমাধান |
|---|---|
| `Login failed for user ...` / DB কানেক্ট হচ্ছে না | `appsettings.json`-এর connection string ঠিক করুন। SQL Server Configuration Manager থেকে **TCP/IP enable** করুন এবং **SQL Server Browser** সার্ভিস চালু আছে কিনা দেখুন |
| `Invalid object name 'dbo.Users'` | `01_Schema.sql` চালানো হয়নি — ধাপ ২ আবার করুন |
| লগইনে `Invalid username or password` | `02_SeedData.sql` চালানো হয়নি, অথবা Email দিয়ে লগইন করার চেষ্টা করছেন — **Username** ব্যবহার করুন |
| Frontend-এ **CORS error** | Backend `http://localhost:5080`-এ চলছে কিনা দেখুন; frontend `4200` ছাড়া অন্য port-এ চললে CORS-এ সেই origin যোগ করুন |
| API কল-এ `401 Unauthorized` | Token-এর মেয়াদ শেষ (৮ ঘণ্টা) — লগআউট করে আবার লগইন করুন |
| নোটিফিকেশন রিয়েলটাইমে আসছে না | SignalR হাব `/hubs/notifications`-এ কানেক্ট হয়েছে কিনা browser console-এ দেখুন |
| `npm install` fail করছে | `node_modules` ও `package-lock.json` ডিলিট করে আবার `npm install` দিন |
| Port 5080 বা 4200 আগে থেকেই ব্যবহৃত | `netstat -ano \| findstr :5080` দিয়ে PID বের করে `taskkill /PID <pid> /F` |
| ভয়েস রেকর্ড হচ্ছে না | ব্রাউজারে মাইক পারমিশন allow করুন; Chrome/Edge ব্যবহার করুন |

---

## 📌 Production-এ যাওয়ার আগে অবশ্যই

- [ ] `appsettings.json`-এর **connection string-এর ইউজার/পাসওয়ার্ড** বদলান (রিপোজিটরিতে যেটা আছে সেটা ডেভ ক্রেডেনশিয়াল)
- [ ] `Jwt:Secret` নতুন strong key দিয়ে বদলান
- [ ] সব default ইউজারের পাসওয়ার্ড বদলান
- [ ] CORS-এ localhost বাদ দিয়ে আসল ডোমেইন দিন
- [ ] Frontend build: `npm run build` (`environment.prod.ts` relative `/api` ব্যবহার করে, তাই একই origin-এ host করলে ভালো)

---

## 📖 আরও জানতে

পুরো functional requirement, task status list, রোলের ক্ষমতা ও screen list বিস্তারিত আছে **`srs_amar_kaj_koi.txt`** ফাইলে।
