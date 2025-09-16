using System.Collections.Generic;
using System.Threading.Tasks;
using StackOverFlowExtractionTool.Models;

namespace StackOverFlowExtractionTool.Services;

public interface IStackOverflowService
{
    Task<List<StackOverflowQuestion>> GetRecentQuestionsByTagAsync(string tag, int page = 1, int pageSize = 20);
}