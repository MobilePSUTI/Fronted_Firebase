using UnityEngine;
using System.Collections.Generic;

public static class UserSession
{
    public static User CurrentUser { get; set; }
    public static Student SelectedStudent { get; set; }
    public static string SelectedGroupId { get; set; }
    public static string SelectedGroupName { get; set; }
}