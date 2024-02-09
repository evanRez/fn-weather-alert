using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.Azure.Functions.Worker.Http;

namespace cl_weatheralert;

public class OutputType
{
    [SqlOutput("dbo.ToDo", connectionStringSetting: "SqlConnectionString")]
    public UserDTO UserDTO { get; set; }
    public HttpResponseData HttpResponse { get; set; }
}
