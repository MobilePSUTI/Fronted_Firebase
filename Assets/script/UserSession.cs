using UnityEngine;
using System.Collections.Generic;

public static class UserSession
{
    public static User CurrentUser { get; set; }
    public static Student SelectedStudent { get; set; }
    public static string SelectedGroupId { get; set; }
    public static string SelectedGroupName { get; set; }

    //êýø
    public static List<Group> CachedGroups = new List<Group>();
    public static Dictionary<string, List<Student>> CachedStudents = new Dictionary<string, List<Student>>();

    public static void ClearCache()
    {
        CachedGroups.Clear();
        CachedStudents.Clear();
    }
}