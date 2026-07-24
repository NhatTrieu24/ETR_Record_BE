namespace ETR.Application.Compliance;

// Authenticated but not permitted (HTTP 403) — distinct from UnauthorizedAccessException (401,
// no/invalid credentials). Conflating the two makes client-side re-auth interceptors fire on a
// case that isn't an auth failure, and blurs the 401-vs-403 signal used to detect IDOR probes.
public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException(string message) : base(message)
    {
    }
}
