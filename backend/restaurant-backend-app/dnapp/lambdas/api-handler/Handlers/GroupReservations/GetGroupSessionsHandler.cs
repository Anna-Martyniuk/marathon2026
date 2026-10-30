using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Services.GuestSessions;
using System.Threading.Tasks;

namespace APIHandler.Handlers.GroupReservations;

public class GetGroupSessionsHandler(IGuestSessionService guestSessionService) : IApiRequestHandler
{
    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (!request.TryGetPathValue("groupReservationId", out string? groupReservationId))
        {
            return ResponseBuilder.CreateErrorResponse(400, "groupReservationId is required in path.", context.Logger);
        }


        try
        {
            var result = await guestSessionService.GetGroupSessionsSnapshotAsync(groupReservationId);
            return ResponseBuilder.CreateJsonResponse(200, result);
        }
        catch (ValidationException exception) // або інші специфічні для вашого проєкту exception
        {
            return ResponseBuilder.CreateErrorCodeResponse(exception);
        }
    }
}