namespace WebAPI.Domain;

public static class PageLimits
{
    public const int MaxPageSize = 50;

    public static int PageNumber(int pageNumber) => pageNumber < 1 ? 1 : pageNumber;

    public static int PageSize(int pageSize)
    {
        if (pageSize < 1)
            return 1;

        return pageSize > MaxPageSize ? MaxPageSize : pageSize;
    }
}
