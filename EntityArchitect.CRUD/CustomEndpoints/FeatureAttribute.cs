using System;

namespace EntityArchitect.CRUD.CustomEndpoints;

public class FeatureAttribute : Attribute
{
    public string Method { get; }
    public string Name { get; }

    public FeatureAttribute(string method, string name)
    {
        Method = method;
        Name = name;
    }
    
    public const string GET = "GET"; 
    public const string POST = "POST";
    public const string PUT = "PUT";
    public const string DELETE = "DELETE";
    public const string PATCH = "PATCH";
    public const string OPTIONS = "OPTIONS";
    public const string HEAD = "HEAD";
}