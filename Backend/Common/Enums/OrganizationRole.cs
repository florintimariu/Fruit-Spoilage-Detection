namespace Backend.Common.Enums;

public enum OrganizationRole
{
    Owner,      // poate adăuga/șterge membri, gestiona organizația
    Operator,   // poate crea/modifica shipments
    Viewer      // doar citire
}