# Kuznyechik (Кузнечик)
Библиотека для шифрования данных с помощью алгоритма "Кузнечик" (ГОСТ Р 34.12-2015), реализующая российский стандарт блочного шифрования. Алгоритм работает с блоками размером 128 бит и использует ключи длиной 256 бит.

## Установка
Для установки библиотеки Kuznyechik вы можете воспользоваться одним из следующих способов:
1. **Установка через NuGet**  
Вы можете установить NuGet-пакет через графический интерфейс Visual Studio или выполнить команду в консоли диспетчера пакетов:

```powerShell
Install-Package Kuznyechik
```
2. **Скачивание DLL**  
Также вы можете скачать DLL-файл с **GitHub** и добавить его в ваш проект.

3. **Сборка из исходников**  
Выполните код в командной строке
```bash
git clone https://github.com/Integeris/Kuznyechik.git
cd Kuznyechik
dotnet build Kuznyechik
```

## Использование
Для использования библиотеки необходимо подключить пространство имён **Kuznyechik**:

```c#
using Kuznyechik;
```

После добавления пространства имён станет доступен класс **Scrambler**, который предоставляет методы шифрования и расшифрования данных как в виде массива, так и в виде потока.

1. Шифрование и расшифрование в виде массива:

```c#
using Kuznyechik;
using System.Text;

...

string text = "Привет, мир!";

// Ключ должен быть именно 32 байта.
byte[] key = new byte[32];
byte[] message = Encoding.UTF8.GetBytes(text);

{
    Random random = new Random();
    random.NextBytes(key);
}

using (Scrambler scrambler = new Scrambler(key))
{
    scrambler.Encrypt(ref message);
    scrambler.Decrypt(ref message);
}

string outText = Encoding.UTF8.GetString(message);
...
```

2. Шифрование и расшифровка в виде потока:

```c#
using Kuznyechik;
using System.Text;
using System.IO;

...

// Ключ должен быть именно 32 байта.
byte[] key = new byte[32];
byte[] message = Encoding.UTF8.GetBytes(text);

{
    Random random = new Random();
    random.NextBytes(key);
}

using (var inputFile = File.OpenRead("document.docx"))
using (var outputFile = File.Create("document.encrypted"))
using (var scrambler = new Scrambler(key))
{
    scrambler.Encrypt(inputFile, outputFile);
}

// Расшифрование обратно
using (var inputFile = File.OpenRead("document.encrypted"))
using (var outputFile = File.Create("document_decrypted.docx"))
using (var scrambler = new Scrambler(key))
{
    scrambler.Decrypt(inputFile, outputFile);
}
...
```

3. Шифрование и расшифрование асинхронно с отслеживанием прогресса:

```c#
using Kuznyechik;
using System.Text;
using System.IO;

...

// Ключ должен быть именно 32 байта.
byte[] key = new byte[32];
byte[] largeData = File.ReadAllBytes("largefile.bin");
byte[] message = Encoding.UTF8.GetBytes(text);

{
    Random random = new Random();
    random.NextBytes(key);
}

var progress = new Progress<CryptoStatus>(status => 
{
    Console.WriteLine($"Обработано: {status.DataPosition} из {status.DataLength} байт");
});

using (Scrambler scrambler = new Scrambler(key))
{
    // Асинхронное шифрование
    var encryptedData = await scrambler.EncryptAsync(largeData, progress);

    // Асинхронная расшифрование
    var decryptedData = await scrambler.DecryptAsync(encryptedData, progress);
}

...
```

## Особенности
1. Использует ILGPU для выполнения операций на GPU;
2. Автоматически дополняет данные до кратности блока;
3. Поддерживает отмену операции.