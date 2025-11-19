using AutoMapper;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MudBlazor;
using SecureVault.App.Application.Features.CQRS.TwoFactorAuthCodes.Commands;
using SecureVault.App.Domain.Enums;
using SecureVault.App.Models.TwoFactorAuthCodeModels;
using SecureVault.App.Resources.Localization;
using SecureVault.App.Services.Interfaces;
using System.Web;

namespace SecureVault.App.Services.Implementations;

public class TwoFactorAuthCodeCreationService : ITwoFactorAuthCodeCreationService
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly ISnackbar _snackbar;
    private readonly ILogger<TwoFactorAuthCodeCreationService> _logger;
    private readonly IStringLocalizer<SharedResources> _localizer;

    public TwoFactorAuthCodeCreationService(
        IMediator mediator,
        IMapper mapper,
        ISnackbar snackbar,
        ILogger<TwoFactorAuthCodeCreationService> logger,
        IStringLocalizer<SharedResources> localizer)
    {
        _mediator = mediator;
        _mapper = mapper;
        _snackbar = snackbar;
        _logger = logger;
        _localizer = localizer;
    }

    public async Task<bool> SubmitCreationCommandAsync(TwoFactorAuthCodeModel model, string logContext)
    {
        _logger.LogInformation("Submitting 2FA code creation command. Context: {Context}", logContext);
        try
        {
            var command = _mapper.Map<CreateTwoFactorAuthCodeCommand>(model);
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _logger.LogInformation("2FA code created successfully. ItemType: {ItemType}", model.Type);
                _snackbar.Add(_localizer[SharedResources.TwoFactorAddedSuccessfully], Severity.Success);
                return true;
            }
            else
            {
                _logger.LogWarning("2FA code creation command failed. Context: {Context}, Error: {ErrorCode} - {ErrorMessage}",
                    logContext, result.Error.Code, result.Error.Message);

                _snackbar.Add(result.Error.Message, Severity.Error);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred during 2FA code creation ({Context}).", logContext);
            _snackbar.Add(_localizer[SharedResources.UnexpectedError], Severity.Error);
            return false;
        }
    }

    public bool TryParseOtpAuthUri(string otpAuthUri, out TwoFactorAuthCodeModel? parsedModel)
    {
        parsedModel = new TwoFactorAuthCodeModel();
        try
        {
            if (!otpAuthUri.StartsWith("otpauth://", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Parsing failed: URI does not start with 'otpauth://'. URI: {UriPrefix}...", otpAuthUri.Substring(0, Math.Min(otpAuthUri.Length, 20)));
                return false;
            }

            var uri = new Uri(otpAuthUri);
            var queryParams = HttpUtility.ParseQueryString(uri.Query);

            parsedModel.Type = uri.Host.Equals("totp", StringComparison.OrdinalIgnoreCase) ? OtpType.TOTP : OtpType.HOTP;

            var secret = queryParams["secret"];
            if (string.IsNullOrEmpty(secret))
            {
                _logger.LogWarning("OTP URI parsing failed: 'secret' parameter is missing.");
                return false;
            }
            parsedModel.SecretKey = secret;

            var path = uri.AbsolutePath.TrimStart('/');
            var issuerFromQuery = queryParams["issuer"];

            _logger.LogDebug("Successfully parsed OTP URI. Type: {Type}, Secret Found: True", parsedModel.Type);

            if (!string.IsNullOrEmpty(issuerFromQuery))
            {
                parsedModel.Issuer = issuerFromQuery;
                parsedModel.AccountName = path.StartsWith(issuerFromQuery)
                    ? path.Substring(issuerFromQuery.Length).TrimStart(':')
                    : path;
            }
            else
            {
                var pathParts = path.Split([':'], 2);
                if (pathParts.Length > 1)
                {
                    parsedModel.Issuer = pathParts[0];
                    parsedModel.AccountName = pathParts[1];
                }
                else
                {
                    parsedModel.AccountName = path;
                }
            }

            if (int.TryParse(queryParams["digits"], out var digits)) parsedModel.Digits = digits;
            if (int.TryParse(queryParams["period"], out var period)) parsedModel.Period = period;
            if (long.TryParse(queryParams["counter"], out var counter)) parsedModel.Counter = counter;

            var algorithmStr = queryParams["algorithm"];
            if (!string.IsNullOrEmpty(algorithmStr) && Enum.TryParse<OtpAlgorithm>(algorithmStr, true, out var algorithm))
                parsedModel.Algorithm = algorithm;

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while parsing OTP URI: {Uri}", otpAuthUri);
            parsedModel = null;
            return false;
        }
    }
}
