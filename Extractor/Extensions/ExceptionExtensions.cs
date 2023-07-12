using System.Net.Sockets;
using System.Security;
using TreeBasedCli.Exceptions;
namespace Extractor.Extensions;

public static class ExceptionExtensions
{
    public static int ExitCode(this Exception ex)
    {
        return ex switch
        {
            MessageOnlyException _ => 2,         // 2 for message only exception
            ArgumentNullException _ => 10,       // 10 for null argument
            ArgumentOutOfRangeException _ => 11, // 11 for argument out of range
            ArgumentException _ => 12,           // 12 for argument exception
            IndexOutOfRangeException _ => 13,    // 13 for index out of range
            NullReferenceException _ => 14,      // 14 for null reference

            InvalidOperationException _ => 20, // 20 for invalid operation
            NotSupportedException _ => 21,     // 21 for not supported exception
            NotImplementedException _ => 22,   // 22 for not implemented exception

            OutOfMemoryException _ => 30,   // 30 for out of memory
            StackOverflowException _ => 31, // 31 for stack overflow
            OverflowException _ => 32,      // 32 for overflow exception

            FileNotFoundException _ => 40,      // 40 for file not found
            DirectoryNotFoundException _ => 41, // 41 for directory not found
            PathTooLongException _ => 42,       // 42 for path too long

            UnauthorizedAccessException _ => 50, // 50 for unauthorized access
            SecurityException _ => 51,           // 51 for security exception

            InvalidCastException _ => 60,     // 60 for invalid cast
            FormatException _ => 61,          // 61 for format exception
            TypeLoadException _ => 62,        // 62 for type load exception
            InvalidTimeZoneException _ => 63, // 63 for invalid time zone exception

            IOException _ => 70,     // 70 for I/O exception
            SocketException _ => 71, // 71 for socket exception

            AggregateException _ => 100, // 100 for aggregate exception

            _ => 1 // 1 for all other exceptions
        };
    }
}
