# Add Any File

This is fork of [**Add New File**](https://marketplace.visualstudio.com/items?itemName=MadsKristensen.AddNewFile) extension which may use local templates.

## Short demo
<p align="center">

</p>
    <a href="https://www.youtube.com/watch?v=9JTRQaODXDA">
        <img src="http://img.youtube.com/vi/9JTRQaODXDA/0.jpg" alt="Local templates demo" height="300"  width = "500"></a>

## How to use

Just create a new `.template` file in your target folder and now any file created using **AddNewFile** command will use this local template instead of global predefined templates like `.cs` or `.html`

This is extremely helpful when you layout your project structure by categories, like `Serviсes`, `Systems`, `Components` etc.

### Tips: 
- `{itemname}` and `{namespace}` tags inside your template will be replaced with the file name.

- **"Exlude From Project"** your template in order not to mess with an actual code. You always can press **Show All Files** button in the **Solution Explorer** to see and modify it.

- For **Unity** users: to prevent Unity from including back you templates simply name it starting with `.` e.g. `.services.template`. This will make your template invisible for Unity. 

## How to install
1) In Visual Studio: Open Extensions > Manage Extensions > Online
2) Print "local templates" and press download

[**Marketplace page**](https://marketplace.visualstudio.com/items?itemName=IlyaSinyavtsev.AddNewFileLocalTemplates)
