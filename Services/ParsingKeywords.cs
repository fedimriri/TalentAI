namespace TalentAI.Services;

/// <summary>
/// Shared keyword lists for resume and job description parsing.
/// Both ResumeParserService and JobParserService MUST use these lists
/// to ensure consistent matching vocabulary.
/// </summary>
public static class ParsingKeywords
{
    /// <summary>
    /// Skill keywords in lowercase for normalized text comparison.
    /// </summary>
    public static readonly string[] SkillKeywords =
    {
        "c#", "java", "python", "javascript", "typescript",
        "angular", "react", "vue", "docker", "kubernetes",
        "sql", "mongodb", "postgresql", "mysql",
        ".net", "asp.net", "azure", "aws", "gcp",
        "git", "rest", "graphql", "node.js",
        "html", "css", "sass", "tailwind",
        "linux", "ci/cd", "jenkins", "terraform",
        "spring", "django", "flask", "express",
        "agile", "scrum", "jira"
    };

    /// <summary>
    /// Display names with original casing, parallel to SkillKeywords.
    /// </summary>
    public static readonly string[] SkillDisplayNames =
    {
        "C#", "Java", "Python", "JavaScript", "TypeScript",
        "Angular", "React", "Vue", "Docker", "Kubernetes",
        "SQL", "MongoDB", "PostgreSQL", "MySQL",
        ".NET", "ASP.NET", "Azure", "AWS", "GCP",
        "Git", "REST", "GraphQL", "Node.js",
        "HTML", "CSS", "Sass", "Tailwind",
        "Linux", "CI/CD", "Jenkins", "Terraform",
        "Spring", "Django", "Flask", "Express",
        "Agile", "Scrum", "Jira"
    };
}
