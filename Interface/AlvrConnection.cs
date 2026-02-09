using Elements.Core;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace QuestProModule;

public class AlvrConnection : IDisposable
{
  private readonly SyncCell<FbMessage> _messageTarget;

  /// <summary>
  /// A message owned by this reader that is currently being mutated.
  /// </summary>
  private FbMessage _workingMessage = new();

  private readonly UdpClient _client;
  private readonly CancellationTokenSource _stopToken = new();
  private readonly Task _listenTask;

  public AlvrConnection(int port, SyncCell<FbMessage> target)
  {
    _messageTarget = target;
    _client = new UdpClient(port);
    _listenTask = Task.Run(ListenAsync);
    UniLog.Log($"[QuestPro4Reso] Opening ALVR connection on {port}");
  }

  private async Task ListenAsync()
  {
    while (!_stopToken.IsCancellationRequested)
    {
      try
      {
        var got = await _client.ReceiveAsync();
        
        // Parse the raw UDP packet directly
        _workingMessage.ParseUdp(got.Buffer);
        
        if (!_stopToken.IsCancellationRequested)
        {
          _messageTarget.Swap(ref _workingMessage);
        }
      }
      catch (Exception ex)
      {
        UniLog.Error(ex.Message);
      }
    }
  }

  public void Dispose()
  {
    UniLog.Log("ALVR connection closing.");
    _stopToken.Cancel();
    _client.Dispose();
  }
}
