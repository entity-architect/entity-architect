using System.Collections.Generic;

namespace EntityArchitect.CRUD.Results.Abstracts;

public class ResultModel
{
    public bool IsSuccess { get; set; }
    public List<Error> Errors { get; set; }
}