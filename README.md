# Implementing Large File Upload Editor for [Serenity Application Platform](https://github.com/volkanceylan/Serenity/) 

1. Support gigabyte size file upload (Chunk upload)
2. Seamless integration with Serenity Application Platform
3. Use the similar setting as MultipleFileUploadEditor

If you don't know what Serenity Application Platform is, please visit https://github.com/volkanceylan/Serenity/ or https://serenity.is/

## How-to
1. Add Microsoft.AspNet.WebApi using Nuget Package Manager

2. Add _WebApiConfig.cs_ in _App_Start_ folder

**WebApiConfig.cs**
```C#
using System.Web.Http;

namespace YourProjectName
{
    public class WebApiConfig
    {
        public static void Register(HttpConfiguration configuration)
        {
            configuration.Routes.MapHttpRoute("API Default", "api/{controller}/{id}",
                new { id = RouteParameter.Optional });
        }
    }
}

```

3. Add `WebApiConfig.Register(GlobalConfiguration.Configuration);` in _Global.asax.cs_ just below `FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);`

4. Download all files in _Modules/Common/LargeFileUpload_ and place in your project (Don't forget to change the namespace)

https://github.com/wezmag/serenity-large-file-upload-sample/tree/master/SereneLargeFileUpload/SereneLargeFileUpload.Web/Modules/Common/LargeFileUpload

5. Build the project, run T4 template and done. Your project is ready for upload gigabyte size files.

## Usage

Please refer to Northwind Region Sample:

https://github.com/wezmag/serenity-large-file-upload-sample/blob/master/SereneLargeFileUpload/SereneLargeFileUpload.Web/Modules/Northwind/Region/RegionRow.cs#L38

https://github.com/wezmag/serenity-large-file-upload-sample/blob/master/SereneLargeFileUpload/SereneLargeFileUpload.Web/Modules/Northwind/Region/RegionForm.cs#L14

**If you run into any issues or find any bugs, please open an issue here.**


