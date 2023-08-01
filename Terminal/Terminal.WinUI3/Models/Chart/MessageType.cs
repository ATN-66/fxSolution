namespace Terminal.WinUI3.Models.Chart;

public enum MessageType
{
    NaN,         // Default or uninitialized value
    Trace,       // Detailed debug information
    Debug,       // Debug-related information
    Information, // Descriptive (normal operation)
    Warning,     // Not critical but notable incidents
    Error,       // Errors that prevent operation
    Critical     // Critical errors causing shutdown
}