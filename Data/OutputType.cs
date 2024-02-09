using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.Azure.Functions.Worker.Http;

namespace data_weatheralert;

public class OutputType
{
    [SqlOutput("dbo.Users", connectionStringSetting: "SqlConnectionString")]
    public UserDTO UserDTO { get; set; }
    public HttpResponseData HttpResponse { get; set; }
}
