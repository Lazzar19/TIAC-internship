namespace WebAPI.Domain;

public class ProductQuerryParametars
{
    private const int MaxPageSize = 50; // 50 so that someone cant request 1000 products at once and crash the server
    private int pageSize_ = 10;

    public int PageNumber { get; set; } = 1;

    public int PageSize
    {
        get => pageSize_;
        set => pageSize_ = value > MaxPageSize ? MaxPageSize : value;
    }
    
    public string? Search { get; set;  }
    public decimal? MinPrice { get; set;  }
    public decimal? MaxPrice { get; set;  }
    
    
}