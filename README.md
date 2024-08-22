# Kuznyechik (Кузнечик)
Библиотека для шифрования данных с помощью алгоритма "Кузнечик", основанного на ГОСТ 34.12-2015. Этот симметричный блочный шифр использует блоки размером 128 бит и ключи длиной 256 бит, обеспечивая высокий уровень безопасности для обработки данных.

## Установка
Для установки библиотеки Kuznyechik вы можете воспользоваться одним из следующих способов:
1. Установка через NuGet
Вы можете установить NuGet-пакет через графический интерфейс Visual Studio или выполнить команду в консоли диспетчера пакетов:

```powerShell
Install-Package Kuznyechik
```
2. Скачивание DLL
Также вы можете скачать DLL-файл с **GitHub** и добавить его в ваш проект.

3. Копирование класса
Если вы предпочитаете, вы можете скопировать исходный код класса **Scrambler** и использовать его непосредственно в вашем проекте.

## Использование
Для использования библиотеки необходимо подключить пространство имён **Kuznyechik**:

```c#
using Kuznyechik;
```

После добавления пространства имён станет доступен класс **Scrambler**, который предоставляет методы шифрования и расшифровывания данных как в виде массива, так и в виде потока.

1. Шифрование и расшифровка в виде массива:

```c#
string text = "Hello Wolrd!";
byte[] key = new byte[32];
byte[] message = Encoding.UTF8.GetBytes(text);

{
    Random random = new Random();
    random.NextBytes(key);
}

Scrambler scrambler = new Scrambler(key);

scrambler.Encrypt(ref message);
scrambler.Decrypt(ref message);

string outText = Encoding.UTF8.GetString(message);
```

2. Шифрование и расшифровка в виде потока:

```c#
byte[] key = new byte[32];
byte[] message = Encoding.UTF8.GetBytes(text);
byte[] messageCopy = (byte[])message.Clone();

using (MemoryStream dataStream = new MemoryStream(message))
{
    using (MemoryStream encryptedStream = new MemoryStream())
    {
        Random random = new Random();
        random.NextBytes(key);

        Scrambler scrambler = new Scrambler(key);
        scrambler.Encrypt(dataStream, encryptedStream);
        dataStream.Position = encryptedStream.Position = 0;
        scrambler.Decrypt(encryptedStream, dataStream);
    }
}
```

## Особенности
1. Ключ шифрования задаётся только при создании экземпляра класса **Scrambler**. Изменить его после создания невозможно.
2. Алгоритм шифрования работает с несколькими блоками одновременно, что может привести к увеличенной загрузке процессора во время шифрования.