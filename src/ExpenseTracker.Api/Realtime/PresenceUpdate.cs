using System.Collections.Concurrent;
using ExpenseTracker.Application.Abstractions.Realtime;
using ExpenseTracker.Application.Common;
using Microsoft.AspNetCore.SignalR;

namespace ExpenseTracker.Api.Realtime;

public sealed record PresenceUpdate(int Online);
