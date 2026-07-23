# FodhelperUacBypass

Tested on:
* Windows 10 
* Windows 11 

---

## How It Works 

1. **Trusted Intermediary (`autoElevate`):** The `fodhelper.exe` utility is officially signed by Microsoft and features a built-in auto-elevate manifest. When an administrator user runs it, Windows **does not display a UAC prompt**, but immediately grants the process a High Integrity level.

2. **Searching for Instructions in HKCU:** Upon startup, `fodhelper.exe` attempts to open system settings and accesses the registry. Due to the specific nature of the Windows hierarchy, the system first looks for configurations in the current user's hive (`HKEY_CURRENT_USER`), and only then in the global hive (`HKEY_LOCAL_MACHINE`).

3. **Class Association Hijacking:** The program intercepts this request by creating the structure `Software\Classes\ms-settings\Shell\Open\command` within the user registry. 
4. **Execution with Inherited Privileges:** Instead of opening the default settings, `fodhelper.exe` reads the path to our program from the created key and executes it. Since `fodhelper.exe` itself already possesses administrative privileges, its new child process **inherits this high integrity level**, successfully bypassing the UAC protection barrier.
---

## Technical Details 

### 1. Current Security Context Check (1. Проверка текущего контекста безопасности)
At startup, the application verifies its current security token. (При старте приложение проверяет свой текущий токен безопасности.) If the program is already running with administrative privileges, execution proceeds directly to the target payload. (Если программа уже запущена с правами администратора, выполнение переходит к целевой нагрузке.) If it has standard privileges, the UAC bypass chain is initiated. (Если права обычные — инициируется цепочка обхода UAC.)

```csharp
public static bool IsTrueAdmin()
```

### 2. String Obfuscation (XOR Encryption) (2. Обфускация строк (XOR-шифрование))
Designed to counter basic static analysis. (Для противодействия базовому статическому анализу)

### 3. Registry Write with Volatile Flag (3. Запись в реестр с флагом Volatile)
The program modifies the user registry hive (`HKCU`). (Программа модифицирует пользовательский куст реестра (`HKCU`).) Additionally, an empty `DelegateExecute` parameter is set, forcing Windows to ignore the standard `ms-settings` command handler and execute the custom string instead. (Дополнительно выставляется пустой параметр `DelegateExecute`, что заставляет Windows игнорировать стандартный обработчик команды `ms-settings` и выполнить кастомную строку)

```csharp
using (RegistryKey key = Registry.CurrentUser.CreateSubKey(regPath, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryOptions.Volatile))
{
    if (key != null)
    {
        key.SetValue("", fullPayload, RegistryValueKind.String);
        key.SetValue("DelegateExecute", "", RegistryValueKind.String);
    }
}
```

### 4. Vulnerability Trigger and Self-Cleanup (4. Триггер уязвимости и самоочистка)
After preparing the registry, `fodhelper.exe` is launched covertly. (После подготовки реестра скрытно стартует `fodhelper.exe`.) The process accesses the created path structure, reads the hijacked command, and launches a new copy of our application within the High Integrity context. (Процесс обращается к созданной структуре путей, считывает подмененную команду и запускает новую копию нашего приложения в контексте High Integrity.) 

```csharp
ProcessStartInfo startInfo = new ProcessStartInfo
{
    FileName = "fodhelper.exe",
    UseShellExecute = true,
    WindowStyle = ProcessWindowStyle.Hidden
};
Process.Start(startInfo);
```

---

## Antivirus Behavior (Поведение Антивируса:)

Despite a clear reaction from the antivirus software, this does not hinder the program's operation. (Несмотря на явную реакцию антивируса это никак не мешает работе программы.) During the detection process, the antivirus flags `fodhelper.exe` itself as the culprit and takes no further action, allowing the program to successfully obtain the required privileges. (В процессе обнаружения антивирус объявляет виновником сам fodhelper.exe и больше ничего не делает, а программа получает нужные права.)

![Antivirus Detection](image.png) *(мое изображение)*

---

## ⚠️ Disclaimer (⚠️ Дисклеймер)
**This project is created strictly for educational purposes.** (**Проект создан исключительно в образовательных целях.**) This source code is provided "as is" for cybersecurity professionals, malware analysts, and system administrators studying operating system defense mechanisms. (Данный исходный код предоставляется «как есть» для ИБ-специалистов, аналитиков вредоносного ПО и системных администраторов, изучающих методы защиты операционных систем.) The author bears no responsibility for any potential damage caused by using this software. (Автор не несет ответственности за любой возможный ущерб, причиненный использованием данного ПО.) Testing is permitted only within isolated sandboxes and testbeds. (Тестирование разрешено проводить только в изолированных песочницах и на тестовых стендах.)
