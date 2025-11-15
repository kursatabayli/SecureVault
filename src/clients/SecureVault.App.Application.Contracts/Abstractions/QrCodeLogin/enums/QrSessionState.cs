namespace SecureVault.App.Application.Contracts.Abstractions.QrCodeLogin.enums;

public enum QrSessionState
{
  Idle,
  CreatingChannel,
  AwaitingPeer,
  AwaitingAuthorization,
  PeerJoined,
  ExchangingKeys,
  SecureChannelEstablished,
  TransferringCredentials,
  Completed,
  Error
}
