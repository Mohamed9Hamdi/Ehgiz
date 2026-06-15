using Ehgiz.Application.DTOs.Review;

public interface IReviewService
{

    Task<List<ReviewDto>> GetByToolAsync(int toolId);


    Task<ReviewDto> GetByIdAsync(int id);


    Task<ReviewDto> CreateAsync(CreateReviewDto dto, int renterId);


    Task DeleteAsync(int id, int renterId);


    Task<double> GetAverageRatingAsync(int toolId);
}