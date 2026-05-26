using BaleManagerSystem.Models;
using System.Collections.Concurrent;

namespace BaleManagerSystem.Services
{
    public class UserStateService
    {
        private readonly ConcurrentDictionary<long, UserState> _states = new();

        public UserState GetOrCreate(long chatId)
        {
            return _states.GetOrAdd(chatId, new UserState());
        }

        public bool TryGet(long chatId, out UserState? state)
        {
            return _states.TryGetValue(chatId, out state);
        }

        public void Remove(long chatId)
        {
            _states.TryRemove(chatId, out _);
        }
    }
}
