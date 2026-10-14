namespace WebAPI.Domain;

public class ProductQuerryParametars
{
    private int pageNumber_ = 1;
    private int pageSize_ = 10;

    public int PageNumber
    {
        get => pageNumber_;
        set => pageNumber_ = PageLimits.PageNumber(value);
    }

    public int PageSize
    {
        get => pageSize_;
        set => pageSize_ = PageLimits.PageSize(value);
    }
    
    public string? Search { get; set;  }
    public decimal? MinPrice { get; set;  }
    public decimal? MaxPrice { get; set;  }
    
    
}