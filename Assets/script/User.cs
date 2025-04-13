public class User
{
    public string Id { get; set; }
    public string Username { get; set; }
    public string First { get; set; }
    public string Last { get; set; }
    public string Second { get; set; }
    public string Email { get; set; }
    public string AvatarPath { get; set; }
    public string CreatedAt { get; set; }
    public string UpdatedAt { get; set; }
    public string Role { get; set; }
}

public class Student : User
{
    public string GroupId { get; set; }
    public string GroupName { get; set; }
    public string SkillId { get; set; }
}

public class Teacher : User
{
    // Дополнительные свойства преподавателя
}