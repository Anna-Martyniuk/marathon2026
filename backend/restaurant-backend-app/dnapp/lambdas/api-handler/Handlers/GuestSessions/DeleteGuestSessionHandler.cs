using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Services.GuestSessions;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace APIHandler.Handlers.GroupReservations;

public class DeleteGuestSessionHandler(IGuestSessionService guestSessionService) : IApiRequestHandler
{
    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        // 1. Отримуємо параметри зі шляху
        if (!request.TryGetPathValue("groupReservationId", out string? groupReservationId) ||
            !request.TryGetPathValue("guestSessionId", out string? guestSessionId))
        {
            return ResponseBuilder.CreateErrorResponse(400, "Missing path parameters.", context.Logger);
        }

        string userId = request.RequestContext.Authorizer?.Claims != null && 
                        request.RequestContext.Authorizer.Claims.TryGetValue("sub", out var claimUserId) 
                        ? claimUserId 
                        : string.Empty;

        if (string.IsNullOrEmpty(userId))
        {
             return ResponseBuilder.CreateErrorResponse(401, "Unauthorized.", context.Logger);
        }

        try
        {
            var result = await guestSessionService.DeleteGuestSessionAsync(userId, groupReservationId, guestSessionId);
            
            return ResponseBuilder.CreateJsonResponse(200, result);
        }
        catch (GuestSessionNotFoundException ex)
        {
            return ResponseBuilder.CreateErrorResponse(404, ex.Message, context.Logger, ex);
        }
        catch (ValidationException ex)
        {
            return ResponseBuilder.CreateErrorCodeResponse(ex);
        }
        catch (UnauthorizedAccessException ex) // Якщо ValidateReservationOwnerAsync кидає таку помилку
        {
            return ResponseBuilder.CreateErrorResponse(403, ex.Message, context.Logger, ex);
        }
        catch (Exception ex)
        {
            return ResponseBuilder.CreateErrorResponse(500, ex.Message, context.Logger, ex);
        }
    }
}