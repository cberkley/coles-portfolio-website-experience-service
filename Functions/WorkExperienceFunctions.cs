using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using PortfolioFunctions.Models;
using PortfolioFunctions.Utility;

namespace PortfolioFunctions.Functions
{
    public class WorkExperienceFunctions
    {
        private readonly ILogger _logger;
        private readonly Container _container;

        public WorkExperienceFunctions(ILoggerFactory loggerFactory, CosmosClient cosmosClient)
        {
            _logger = loggerFactory.CreateLogger<WorkExperienceFunctions>();

            var databaseName = Environment.GetEnvironmentVariable("CosmosDbDatabaseName");
            var containerName = Environment.GetEnvironmentVariable("CosmosDbWorkExperienceContainerName");
            _container = cosmosClient.GetContainer(databaseName, containerName);
        }

        [Function("GetWorkExperiences")]
        [OpenApiOperation(operationId: "GetWorkExperiences", tags: new[] { "WorkExperiences" }, Summary = "Get all work experiences", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<WorkExperience>), Description = "List of work experiences")]
        public async Task<HttpResponseData> GetWorkExperiences(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "work-experiences")] HttpRequestData req)
        {
            _logger.LogInformation("Retrieving work experiences from Cosmos DB.");

            var workExperiences = new List<WorkExperience>();
            var query = new QueryDefinition("SELECT * FROM c");

            using var iterator = _container.GetItemQueryIterator<WorkExperience>(query);
            while (iterator.HasMoreResults)
            {
                var page = await iterator.ReadNextAsync();
                workExperiences.AddRange(page);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(workExperiences);
            return response;
        }

        [Function("GetWorkExperienceById")]
        [OpenApiOperation(operationId: "GetWorkExperienceById", tags: new[] { "WorkExperiences" }, Summary = "Get a work experience by ID", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "Work experience ID")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(WorkExperience), Description = "The requested work experience")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Work experience not found")]
        public async Task<HttpResponseData> GetWorkExperienceById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "work-experiences/{id}")] HttpRequestData req,
            string id)
        {
            _logger.LogInformation("Retrieving a work experience by id from Cosmos DB.");

            try
            {
                var response = await _container.ReadItemAsync<WorkExperience>(id, new PartitionKey(id));
                var result = req.CreateResponse(HttpStatusCode.OK);
                await result.WriteAsJsonAsync(response.Resource);
                return result;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                await notFound.WriteStringAsync($"Work experience with id '{id}' not found.");
                return notFound;
            }
        }

        [Function("AddWorkExperience")]
        [OpenApiOperation(operationId: "AddWorkExperience", tags: new[] { "WorkExperiences" }, Summary = "Add a new work experience", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiSecurity("x-ms-client-principal-id", SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Header, Name = "X-MS-CLIENT-PRINCIPAL-ID")]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(WorkExperience), Required = true, Description = "The work experience to create")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(WorkExperience), Description = "The created work experience")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = "Invalid work experience data")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Unauthorized")]
        public async Task<HttpResponseData> AddWorkExperience(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "work-experiences")] HttpRequestData req)
        {
            if (!AuthHelper.IsAuthenticated(req))
                return req.CreateResponse(HttpStatusCode.Unauthorized);

            _logger.LogInformation("Adding a work experience to Cosmos DB.");

            var workExperience = await req.ReadFromJsonAsync<WorkExperience>();
            if (workExperience == null || string.IsNullOrWhiteSpace(workExperience.Company))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("A work experience with at least a 'company' is required.");
                return badRequest;
            }

            if (string.IsNullOrWhiteSpace(workExperience.Id))
            {
                workExperience.Id = Guid.NewGuid().ToString();
            }

            var created = await _container.CreateItemAsync(
                workExperience,
                new PartitionKey(workExperience.Id));

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(created.Resource);
            return response;
        }

        [Function("UpdateWorkExperience")]
        [OpenApiOperation(operationId: "UpdateWorkExperience", tags: new[] { "WorkExperiences" }, Summary = "Update an existing work experience", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiSecurity("x-ms-client-principal-id", SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Header, Name = "X-MS-CLIENT-PRINCIPAL-ID")]
        [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "Work Experience ID")]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(WorkExperience), Required = true, Description = "The updated work experience data")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(WorkExperience), Description = "The updated work experience")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = "Invalid work experience data")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Work experience not found")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Unauthorized")]
        public async Task<HttpResponseData> UpdateWorkExperience(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "work-experiences/{id}")] HttpRequestData req,
            string id)
        {
            if (!AuthHelper.IsAuthenticated(req))
                return req.CreateResponse(HttpStatusCode.Unauthorized);

            _logger.LogInformation("Updating a work experience in Cosmos DB.");

            var updatedWorkExperience = await req.ReadFromJsonAsync<WorkExperience>();
            if (updatedWorkExperience == null || string.IsNullOrWhiteSpace(updatedWorkExperience.Company))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("A work experience with at least a 'company' is required.");
                return badRequest;
            }

            updatedWorkExperience.Id = id;

            try
            {
                var response = await _container.ReplaceItemAsync(
                    updatedWorkExperience,
                    id,
                    new PartitionKey(id));

                var result = req.CreateResponse(HttpStatusCode.OK);
                await result.WriteAsJsonAsync(response.Resource);
                return result;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                await notFound.WriteStringAsync($"Work experience with id '{id}' not found.");
                return notFound;
            }
        }

        [Function("DeleteWorkExperience")]
        [OpenApiOperation(operationId: "DeleteWorkExperience", tags: new[] { "WorkExperiences" }, Summary = "Delete a work experience", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiSecurity("x-ms-client-principal-id", SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Header, Name = "X-MS-CLIENT-PRINCIPAL-ID")]
        [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "Work experience ID")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent, Description = "Successfully deleted")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Work experience not found")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Unauthorized")]
        public async Task<HttpResponseData> DeleteWorkExperience(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "work-experiences/{id}")] HttpRequestData req,
            string id)
        {
            if (!AuthHelper.IsAuthenticated(req))
                return req.CreateResponse(HttpStatusCode.Unauthorized);

            _logger.LogInformation("Deleting a work experience from Cosmos DB.");

            try
            {
                var response = await _container.DeleteItemAsync<WorkExperience>(id, new PartitionKey(id));
                var result = req.CreateResponse(HttpStatusCode.NoContent);
                return result;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                await notFound.WriteStringAsync($"Work experience with id '{id}' not found.");
                return notFound;
            }
        }
    }
}
