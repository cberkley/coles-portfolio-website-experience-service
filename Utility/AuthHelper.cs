using Microsoft.Azure.Functions.Worker.Http;

namespace PortfolioFunctions.Utility
{
    public static class AuthHelper
    {
        // I am the only user who needs elevated permissions I realize this is not enough for a 
        // production application but it is enough for my personal portfolio. I will be the only 
        // user who needs elevated permissions so I am just checking if the user is authenticated 
        // and not checking if they are authorized to perform any specific actions.
        public static bool IsAuthenticated(HttpRequestData req) =>
            req.Headers.TryGetValues("X-MS-CLIENT-PRINCIPAL-ID", out var values) &&
            !string.IsNullOrWhiteSpace(System.Linq.Enumerable.FirstOrDefault(values));
    }
}
