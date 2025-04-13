using System.Collections.Generic;

public static class NewsDataCache
{
    public static List<Post> CachedPosts { get; set; }
    public static Dictionary<long, VKGroup> CachedVKGroups { get; set; }
}
