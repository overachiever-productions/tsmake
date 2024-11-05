namespace tsmake;

public enum VersionScheme
{
    FourPart,
    Semantic,
    Custom
}

public enum PathType
{
    NotSet,
    Absolute,
    Relative,
    Rooted
}

public enum OrderBy
{
    Alphabetical,
    ModifyDate,
    CreateDate
}

public enum Direction
{
    Ascending,
    Descending
}

[Flags]
public enum CommentType
{
    None = 1,
    LineEndComment = 2,
    BlockComment = 4
}