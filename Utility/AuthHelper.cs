using Microsoft.Azure.Functions.Worker.Http;

namespace ProjectsServiceFuncitons.Utility
{
    public static class AuthHelper
    {
        public static bool IsAuthenticated(HttpRequestData req)
        {
            var expectedPrincipalId = Environment.GetEnvironmentVariable("AdminClientPrincipalId");
            if (string.IsNullOrWhiteSpace(expectedPrincipalId))
            {
                return false;
            }

            if (!req.Headers.TryGetValues("X-MS-CLIENT-PRINCIPAL-ID", out var values))
            {
                return false;
            }

            var principalId = System.Linq.Enumerable.FirstOrDefault(values);
            if (string.IsNullOrWhiteSpace(principalId))
            {
                return false;
            }

            return string.Equals(
                principalId.Trim(),
                expectedPrincipalId.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
