# Kuznyechik (Кузнечик)

Библиотека для шифрования данных с помощью алгоритма "Кузнечик" (ГОСТ Р 34.12-2015 / [ГОСТ 34.12-2018](https://meganorm.ru/Data2/1/4293732/4293732907.pdf)), реализующая российский стандарт блочного шифрования. Алгоритм работает с блоками размером 128 бит и использует ключи длиной 256 бит.

## Особенности

- Реализация алгоритма "Кузнечик" в полном соответствии с ГОСТ 34.12-2015/2018
- Работа с блоками 128 бит и ключами 256 бит
- Поддержка синхронного и асинхронного шифрования
- Работа с массивами байтов и потоками (Stream)
- Автоматическое дополнение данных до кратности блока
- Отслеживание прогресса операций
- Поддержка отмены операций через CancellationToken

## Установка

### Установка через NuGet
```powershell
Install-Package Kuznyechik
```

### Сборка из исходников

```bash
git clone https://github.com/Integeris/Kuznyechik.git
cd Kuznyechik
cd Kuznyechik
dotnet build
```

## Быстрый старт

### Базовое шифрование и расшифрование массива байтов

```csharp
using Kuznyechik;
using System.Text;

string text = "Привет, мир!";
byte[] key = new byte[32]; // 256 бит
byte[] message = Encoding.UTF8.GetBytes(text);

// Генерация случайного ключа
Random random = new Random();
random.NextBytes(key);

// Создание шифровальщика
Scrambler scrambler = new Scrambler(key);

// Шифрование
scrambler.Encrypt(ref message);
Console.WriteLine($"Зашифровано: {Convert.ToBase64String(message)}");

// Расшифрование
scrambler.Decrypt(ref message);
string decryptedText = Encoding.UTF8.GetString(message);
Console.WriteLine($"Расшифровано: {decryptedText}");
```

### Работа с файлами через потоки

```csharp
using Kuznyechik;
using System.IO;

// Генерация ключа
byte[] key = new byte[32];
Random random = new Random();
random.NextBytes(key);

// Создание шифровальщика
Scrambler scrambler = new Scrambler(key);

// Шифрование файла
using (var inputFile = File.OpenRead("document.docx"))
using (var outputFile = File.Create("document.encrypted"))
{
    scrambler.Encrypt(inputFile, outputFile);
}

// Расшифрование файла
using (var inputFile = File.OpenRead("document.encrypted"))
using (var outputFile = File.Create("document_decrypted.docx"))
{
    scrambler.Decrypt(inputFile, outputFile);
}
```

### Асинхронное шифрование с отслеживанием прогресса

```csharp
using Kuznyechik;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

byte[] key = new byte[32];
Random random = new Random();
random.NextBytes(key);

Progress<CryptoStatus> progress = new Progress<CryptoStatus>(status => 
{
    Console.WriteLine($"Обработано: {status.DataPosition} из {status.DataLength} байт");
});

CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
Scrambler scrambler = new Scrambler(key);

try
{
    // Асинхронное шифрование
    var largeData = await File.ReadAllBytesAsync("largefile.bin");
    var encryptedData = await scrambler.EncryptAsync(
        largeData, 
        progress, 
        cancellationTokenSource.Token
    );
    
    // Асинхронное расшифрование
    var decryptedData = await scrambler.DecryptAsync(
        encryptedData, 
        progress, 
        cancellationTokenSource.Token
    );
    
    await File.WriteAllBytesAsync("largefile_decrypted.bin", decryptedData);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Операция отменена.");
}
```

## Особенности реализации
- Использует структуры для минимального выделения памяти
- Применяет unsafe-код для повышения производительности
- Потокобезопасный при использовании отдельных экземпляров Scrambler
- Использует метод быстрого шифрования за счёт предвычисленных таблиц