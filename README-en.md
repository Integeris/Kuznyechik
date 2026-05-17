# Kuznyechik (Кузнечик)

A library for data encryption using the "Kuznyechik" algorithm (GOST R 34.12-2015 / [GOST 34.12-2018](https://meganorm.ru/Data2/1/4293732/4293732907.pdf)), implementing the Russian standard for block encryption. The algorithm operates with 128-bit blocks and uses 256-bit keys.

## Features

- Implementation of the "Kuznyechik" algorithm fully compliant with GOST 34.12-2015/2018
- 128-bit block and 256-bit key support
- Support for synchronous and asynchronous encryption
- Works with byte arrays and streams
- Automatic data padding to block size
- Operation progress tracking
- Support for operation cancellation via CancellationToken

## Installation

### Installation via NuGet
```powershell
Install-Package Kuznyechik
```

### Building from source

```bash
git clone https://github.com/Integeris/Kuznyechik.git
cd Kuznyechik
cd Kuznyechik
dotnet build
```

## Quick Start

### Basic encryption and decryption of a byte array

```csharp
using Kuznyechik;
using System.Text;

string text = "Привет, мир!";
byte[] key = new byte[32]; // 256 bits
byte[] message = Encoding.UTF8.GetBytes(text);

// Generate a random key
Random random = new Random();
random.NextBytes(key);

// Create an encryptor
Scrambler scrambler = new Scrambler(key);

// Encryption
scrambler.Encrypt(ref message);
Console.WriteLine($"Зашифровано: {Convert.ToBase64String(message)}");

// Decryption
scrambler.Decrypt(ref message);
string decryptedText = Encoding.UTF8.GetString(message);
Console.WriteLine($"Расшифровано: {decryptedText}");
```

### Working with files via streams

```csharp
using Kuznyechik;
using System.IO;

// Generate a key
byte[] key = new byte[32];
Random random = new Random();
random.NextBytes(key);

// Create an encryptor
Scrambler scrambler = new Scrambler(key);

// Encrypt a file
using (var inputFile = File.OpenRead("document.docx"))
using (var outputFile = File.Create("document.encrypted"))
{
    scrambler.Encrypt(inputFile, outputFile);
}

// Decrypt a file
using (var inputFile = File.OpenRead("document.encrypted"))
using (var outputFile = File.Create("document_decrypted.docx"))
{
    scrambler.Decrypt(inputFile, outputFile);
}
```

### Asynchronous encryption with progress tracking

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
    // Asynchronous encryption
    var largeData = await File.ReadAllBytesAsync("largefile.bin");
    var encryptedData = await scrambler.EncryptAsync(
        largeData, 
        progress, 
        cancellationTokenSource.Token
    );
    
    // Asynchronous decryption
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

## Implementation Features
- Uses structures for minimal memory allocation
- Employs unsafe code for performance optimization
- Thread-safe when using separate Scrambler instances
- Uses a fast encryption method based on precomputed tables