using Newtonsoft.Json;

namespace LocusPocusBot
{
    public static class ObjectExtensions
    {
        public static string ToJson(this object update)
        {
            return JsonConvert.SerializeObject(update);
        }
    }
}
