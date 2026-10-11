# Розгортання на Amazon EC2 (Windows Server)

Сервер разом із вебклієнтом публікується як один самодостатній застосунок: на віртуальній
машині не потрібно встановлювати .NET. Після розгортання за однією адресою працюють і
вебверсія, і REST API, до якого підключається десктоп-клієнт.

## 1. Зібрати застосунок (на своєму комп'ютері)

```powershell
powershell -ExecutionPolicy Bypass -File publish.ps1 -Runtime win-x64
Compress-Archive -Path publish\* -DestinationPath MyDbShumeiko.zip -Force
```

## 2. Створити віртуальну машину

У консолі AWS: **EC2 → Launch instance**.

- **AMI:** Microsoft Windows Server 2022 Base.
- **Instance type:** `t3.micro` (або `t2.micro`) — перевірте, що тип позначено як
  *Free tier eligible* для вашого облікового запису.
- **Key pair:** створіть і збережіть файл `.pem` — ним розшифровується пароль адміністратора.
- **Security group** (вхідні правила):
  - RDP, TCP 3389, джерело — *My IP*;
  - HTTP, TCP 80, джерело — *Anywhere* (`0.0.0.0/0`).

## 3. Скопіювати файли

1. **EC2 → Instances → Connect → RDP client → Get password**, розшифруйте пароль файлом `.pem`.
2. Підключіться через «Підключення до віддаленого робочого стола».
3. Скопіюйте `MyDbShumeiko.zip` на машину (копіювання й вставлення через буфер обміну RDP)
   і розпакуйте, наприклад, у `C:\MyDbShumeiko`.

## 4. Відкрити порт і запустити (PowerShell від імені адміністратора, на EC2)

```powershell
New-NetFirewallRule -DisplayName "MyDbShumeiko" -Direction Inbound -Protocol TCP -LocalPort 80 -Action Allow
cd C:\MyDbShumeiko
.\MyDbShumeiko.Api.exe --urls http://0.0.0.0:80
```

Перевірка: відкрийте у своєму браузері `http://<публічна-адреса-EC2>/` — має завантажитися
вебклієнт. Документація API: `http://<публічна-адреса-EC2>/swagger`.

## 5. Автозапуск (щоб сервер працював без відкритого сеансу RDP)

Зупиніть процес із кроку 4 (`Ctrl+C`) і зареєструйте завдання, яке стартує разом із системою:

```powershell
$action  = New-ScheduledTaskAction -Execute "C:\MyDbShumeiko\MyDbShumeiko.Api.exe" `
           -Argument "--urls http://0.0.0.0:80" -WorkingDirectory "C:\MyDbShumeiko"
$trigger = New-ScheduledTaskTrigger -AtStartup
Register-ScheduledTask -TaskName "MyDbShumeiko" -Action $action -Trigger $trigger -User "SYSTEM" -RunLevel Highest
Start-ScheduledTask -TaskName "MyDbShumeiko"
```

Робоча тека (`-WorkingDirectory`) обов'язкова: відносно неї сервер шукає `wwwroot`
і створює теку `data` з файлами баз.

## 6. Підключити клієнти

- **Вебверсія:** `http://<публічна-адреса-EC2>/`.
- **Десктоп-клієнт:** у полі «Сервер» введіть `http://<публічна-адреса-EC2>` і натисніть
  «Підключитися», або запустіть клієнт з адресою як аргументом.

## Що варто знати

- **Адреса змінюється** після зупинки й повторного запуску машини. Щоб вона була сталою,
  прив'яжіть до машини Elastic IP.
- **Доступу ніхто не обмежує:** автентифікації в системі немає, тож будь-хто, хто знає адресу,
  може змінювати дані. Для навчальної демонстрації це прийнятно, але після захисту машину
  краще зупинити або видалити.
- **Лише HTTP.** Для HTTPS потрібні доменне ім'я й сертифікат.
- **Вартість:** стежте за лімітами пробного періоду в розділі Billing; зупинена машина
  не споживає годин обчислень, але диск і Elastic IP можуть тарифікуватися.
- **Дані** лежать у `C:\MyDbShumeiko\data\*.json`; це звичайні файли, їх можна копіювати як резервні копії.
