namespace Backend.Common.Enums;

public enum ShipmentStatus
{
    Created,        // creat dar nu a început
    InProgress,     // are step activ
    Completed,      // ajuns la destinație
    Compromised     // detectat ca stricat
}