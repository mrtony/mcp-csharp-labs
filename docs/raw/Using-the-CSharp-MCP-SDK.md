# Using the C# Model Context Protocol SDK

## Getting Started with the MCP SDK

### Introduction

Hi there, and welcome to the course. We're going to be on a journey together to learn about using the Model Context Protocol, or **MCP**, with the C# SDK. Using MCP is a great way to expose the functionality of your custom applications to AI services, whether those are chatbots, semi or fully autonomous agents, or even combinations of them in complex workflows. My name is Erik, and I'm glad you're on this journey with me. Without any further delay, let's jump in, and I'll tell you what you can expect.

The primary focus of this course will be to create an MCP server that will expose some features of an application that's already been built to AI services. There are two different types of MCP servers you can create:

- **Remote**, which uses Streamable HTTP as a transport — this is what we'll use for our primary use case.
- **Local**, which uses standard IO (input and output) as its transport.

We'll look at both of these and explain them each in more detail, but we'll definitely spend more time with the remote flavor since that's likely the one you're most interested in. Our application has an API inside it already, and we'll definitely use those routes and services in our remote MCP server.

We'll also look at some security considerations that you should keep in mind as well, both how to limit what can be done within an MCP server controlled by AI and how to see who it is that's doing things — whether it's AI on its own or on behalf of a user. Once we've got our server created and can test it in different ways, we'll see how to use it from an AI agent within our application.

All of this will be pretty fast-paced and demo-focused. If you've had some experience with MCP before, this should round out your knowledge a bit more and won't waste your time. And if you're newer to MCP, full explanations are here, and the clips will be short enough and well-labeled so that you can rewatch anything you want repeated. I'll be focusing mostly on demos in code and only use slides when they're appropriate to our journey.

### Requirements

The best way for you to learn about this is to be working with the code alongside me. To do that, you should have the following in place:

- The **.NET SDK** installed and an **IDE** of your choice. I'll be using Visual Studio, but VS Code and Rider will work equally well. Note that you'll need the **C# DevKit** extension installed if you're using VS Code.
- **Aspire**, which we'll use to greatly simplify our local developer experience. So if you're using Rider, make sure you have the **.NET Aspire plugin** installed.
- A **container runtime**. Docker Desktop is the most common one, but I'll be using Rancher Desktop. Podman is another alternative that should also work just fine.

If you've been doing .NET development already, you probably have these in place. And if you'd like a deeper dive into Aspire either before or after some of the things you see here, I've got another course on Pluralsight — two actually — covering that for both development and deployment scenarios.

### The C# MCP SDK

To use the C# MCP SDK, you'll most likely be installing one of two NuGet packages:

- `ModelContextProtocol` — the simpler package, used for local standard IO-based servers.
- `ModelContextProtocol.AspNetCore` — provides additional functionality for us to wire our MCP servers into the ASP.NET Core pipelines for services and middleware.

Both of these NuGet packages are still officially in prerelease status as I'm creating this course, but they're usable enough for us to be talking concretely about them. Changes will almost certainly occur before their official 1.0 non-prerelease versions, but you'll know enough after watching this to tackle any changes needed.

The best place to see up-to-date information about the SDK is in the **GitHub repo** for it. It's very active — you can see the latest changes happened 4 days ago, and it's got samples, tests, and the source code itself. If you run into trouble with anything that you're doing beyond what you'll see in this course, you can search through the issues and pull requests here and even engage in the conversation. The README has more information that we'll be looking at soon. API documentation exists, but this is generated content and will help you see what methods and properties exist; it may not give you a lot of context about how to use them. There's also a pretty good page in **Microsoft Learn** about the C# MCP SDK, in the AI tools and SDKs section of AI for .NET developers. The blurb at the top there explains that MCP is built to standardize integrations between AI apps and external services and data.

Now let's get a quick bird's eye view of the features that are in an MCP server so that we're better grounded when we start with the demos:

- **Tools** — the most common thing you'll see. These are the custom code that can get executed by an AI service. A tool might call one or more APIs, make database queries, or perform computations or execute other discrete logic.
- **Resources** — an MCP server can make resources available to AI. This might be a file, a database schema, or other kind of URI.
- **Prompts** — more targeted prompts for things like slash commands or prompts that have a very specific purpose within a larger context.

Beyond making tools, resources, and prompts available for AI services, an MCP server has a **transport protocol**, and this can be either:

- **Standard IO** (standard input and output), like a console application — something that would run on the user's machine.
- **Streamable HTTP** — the remote server would be hosted on some web server somewhere.

Finally, **security** can be applied to both of these protocols. For remote servers using ASP.NET Core, it's common to use OAuth2 since remote calls here are very similar to API calls. But now, without any further delay, let's create our first MCP server.

### Demo: Creating an MCP Server (STDIO)

Let's create our first MCP server. A good place to start is in the GitHub repo for the SDK, and it's got a good README with some information. There's a Getting Started section for the client, which will come in handy later when we talk about tests. But for now, let's skip that section and head down to the Getting Started section for a server. There are a couple of NuGet packages listed here — note the `ModelContextProtocol` one and its prerelease arg, and then `Microsoft.Extensions.Hosting`.

I'm using Visual Studio as my IDE, and we need to create a brand new console application: **New Project → Console App**, and I'll name it `Hello.Mcp`. The code for this will be in the exercise files and in the GitHub repo. It's just a console app, and these other options here are correct, so I'll click **Create**.

I'll get rid of the existing code, and then let's add those two NuGet packages we saw in the README. If you search for `ModelContextProtocol`, you'll find some stuff, but not the package we're looking for — you need the **Include prerelease** checkbox checked. We want `ModelContextProtocol` for this demo, so I'll install that. Then, let's look for `Microsoft.Extensions.Hosting`, and we don't need this to be a prerelease version. I'll install it too.

Okay, with that done, let's go back to our empty `Program.cs` file, and I'll paste in the code we copied. Let's review this quickly:

1. First thing is to create an **application builder** — not a web application, just application. That will allow us to use dependency injection and the services collection.
2. We set up all logging down to the trace level to go to the console, which is part of the standard input and output of standard IO.
3. Then we use the services collection to add MCP and the **standard IO transport** protocol for it.
4. Then we add tools from this assembly. This line will scan the code in this assembly for classes that have the `McpServerToolType` attribute on it. And it just so happens that we have such a class here.
5. After that, the builder object is built into an application and run.

The code below is that class with tools. It just has one tool, called `echo`, and it will apparently echo a message back to the client. There's a `message` parameter to the tool, and it returns a string that includes the word hello in front of whatever was passed.

Let's build it. Running it isn't as simple as just hitting **F5** — well, it is, but interacting with it is trickier than that. Once it's built, we need a way to interact with our new MCP server. This is a console app, which is an executable (`.exe`) file. I'll copy the file name that's described in this build output but without the extension.

We can use **Postman** to interact with our new MCP server. You can download Postman, and it's primarily used as an API client. In my workspace, I can choose **New** to create a new request, and I have lots of options, one of which is **MCP**. Let's choose that one. Now I've got a new request, and you can see there are options for standard IO and HTTP. I'll leave it as standard IO for this one. Then I can paste in that file name and add the `.exe` extension. This was created when we built our new MCP server. Then, we can click **Connect**. There's a permission question, and I'll click **OK**.

Then we can see a view with **Tools**, **Prompts**, and **Resources**, and we can see our `echo` tool in the Tools tab. The description there came straight from the attribute on the `echo` method. The Prompts and Resources tabs are empty since we didn't add any of those. So let's hit this Tools tab some more. When I click the `echo` tool, it shows an input field where I can provide a message as an input parameter. I'll type `MCP` and then run the tool and check out the results. It's `hello MCP`, just like you'd probably expected. In the results area, there's a JSON view and a preview view.

Let's make a couple of changes. I'm going to paste in some code for a second tool method — a method that will return a dictionary of `int` and `int` that will be the first *n* Fibonacci numbers where *n* is an input parameter to the method. I've got a log entry we want to create in this method, so the method can't be a static one. That means I also need to change the class to not be static, and we should probably change the class name from `EchoTool` to `FirstTools` to be more descriptive. Then I'll use a primary constructor to inject an `ILogger` of type `FirstTools`. That clears up the errors from when I pasted this new code in. And now, we have two tools in this one class.

You can explore and review the code to create the dictionary, but this code isn't really the point of the demo. Note the attribute on the new method though — we can specify a **name** for the tool, and this will override the method name, which is `GetFibonacciNumbers`. There's a **description** again, and this is an important aspect of tools and MCP servers.

Let's put a breakpoint in this function and build it. I'm seeing some errors that it can't access the file, and this is because we didn't disconnect from the `.exe` in our Postman client. I'll click the **Disconnect** button by the Run button. Now let's try building again — it works very quickly this time. Let's go back to Postman, click **Connect**, and now we see both tools in the Tools tab. I'll click **Fibonacci** (note this shorter name that came from the attribute and not the method name).

We had a breakpoint set. To actually debug it, we need to attach to the running process. In Visual Studio, we can choose **Debug → Attach to Process**, and then look for our `HelloMcp.exe` program. There it is, and I'll click **Attach**. Back in Postman, I'll provide `8` as the number and then run the tool, and we hit our breakpoint. I'll just continue, and the tool is showing some results. These are easier to see in the Preview tab, and sure enough, it's the first 8 Fibonacci numbers. If you wanted to see the log entries, which includes that new one from this method, you can look at the console area within Postman. Scroll down to the bottom, and sure enough, there's that entry that says we're calculating the first 8 Fibonacci numbers.

So we've created an MCP server, connected to it via Postman, executed tools, added tools, debugged it, and saw log entries. We're off to a great start.

### Demo: Creating an MCP Server Using ASP.NET Core (HTTP)

Now let's create an MCP server with ASP.NET Core, one that can call a simple API. In the C# MCP SDK repo, there's a folder for **samples**. You can explore these on your own — it's definitely worth it. But let's have a peek at the `AspNetCoreMcpServer`. `Program.cs` here has some familiar elements if you're used to working with ASP.NET Core.

To create a great developer experience where we can interact with our server without more installs and see logs and other such things more easily, let's use **Aspire**. I'll create a brand new project using the **.NET Aspire Starter app**. I'll call it `Hello.AspNetCoreMcp` and hit **Next**. We don't need Redis or a test project for now, so I'll hit **Create**. This has created an Aspire solution with four projects: an API, the Aspire app host, the ServiceDefaults class library, and a web front end. We won't use the web front end, so I'll delete that project. In the app host, I'll delete the code that referred to the web front end. That leaves the app host with just an API in it. That API has a single minimal API route called `WeatherForecast` that doesn't take any parameters. So our solution here is really simple.

Let's add a new empty ASP.NET Core project that we can put our MCP server in: **ASP.NET Core Empty**, and I'll call it `Hello.AspNetCoreMcp.McpServer`. We can leave the box checked to add it to the Aspire orchestration. Now let's add the prerelease `ModelContextProtocol.AspNetCore` package.

Now, let's paste in that code from the samples folder from the SDK repo. This isn't using Aspire, so I'll take most of this code and be a little more careful about pasting it in. We want to keep the `AddServiceDefaults` and `MapDefaultEndpoints` calls in place. We don't need any of the OpenTelemetry stuff — that's all included in that `AddServiceDefaults` method — and we'll only keep the weather tool for now. I'll move `MapDefaultEndpoints` above the `app.MapMcp` call, and we're getting close. I'll get rid of the unnecessary usings, and now we just have this `WeatherTools` problem to deal with.

I'll add a new class using **Shift+F2** called `Tools/WeatherTools.cs`, and it's empty. Let's grab the content from that samples repo — the Tools folder, and here's `WeatherTools.cs`. I'll grab the code for the whole class and paste it into our new project. Everything looks good. There's a class with the `McpServerToolType` attribute on it, and it's got an `HttpClientFactory` being injected by constructor. Then it's got a tool method called `GetAlerts`. Note the `Description` attribute preceding the `string state` parameter. Inside the method, it uses the `HttpClientFactory` to get a WeatherApi client, makes a call to the API, and parses the document for some output.

Let's add a tool into this set that calls the `WeatherForecast` endpoint in the API project of this solution. In the Aspire app host, I'll add a `WithReference` line so that the MCP server will be able to use service discovery to connect to our API. I'll also add a health check here for consistency. In `Program.cs` for the MCP server, I'll paste in the code that will create a named `HttpClient` for our API service.

Now we can add a using statement to resolve the `WeatherTools` error, and note that this is not using reflection to add all of the tool types from this assembly, but rather explicitly adding specific types. We'll see other options for determining which tools to add later as well. So now, let's add a tool in the `WeatherTools` class that will use this client and call the `WeatherForecast` API. I'll add a `JsonSerializerOptions` variable so that we don't need to create a new one with each request, and then I'll paste in the code for our new tool. It creates a simple weather `HttpClient`, then calls the `WeatherForecast` endpoint and deserializes the result. It handles a null response and then returns the JSON version of the same. Let's add a record we can use for this deserialization.

You might be wondering why we deserialize the result from the API only to serialize it right back. It's a good question, and the answer is that it's just a sample. You may want to add, remove, rename, or update some of the content, and it might be easier to do that against C# objects rather than strict JSON content. The main point to realize is that the responses probably depend on what your AI service can use best.

So now, we've got an MCP server in our application with three tools. We need a way to interact with it, and to do that, we can use the **Aspire hosting integration package for MCP Inspector**. I'll right-click the app host project, choose **Add → .NET Aspire Package**. This is just a way to provide some additional filtering when adding a NuGet package. At the end of this pre-formatted search, I'll add `MCP`, and here's the community toolkit for the MCP Inspector. I'll add that, and I'm adding the prerelease version too.

Then in the app host code, I'll change the name of this MCP server to simplify it a bit, and I'll capture a reference to it in a variable. Then we can call `builder.AddMcpInspector`, give this service a name of **MCP Inspector** that will show up in the Aspire dashboard, and then give it an MCP server of our MCP instance.

Let's build and run this. Here's the dashboard, and it's got our API, our MCP server, and the MCP Inspector running. To see things in action, let's use the MCP Inspector — I'll click the link for the client. Note that the HTTP port for our MCP server is `5088`. Here's a UI for the inspector; the transport type is already **HTTP**, and the URL is for that `5088` port. Let's click **Connect**.

Shoot — when I did that, I got an error saying that our MCP server might not be running or we might have a bad proxy token. Those seem like bigger and intimidating problems, but the solution here is actually a lot simpler. Our MCP server is not mapped to a path of `/mcp`. So if we just remove `mcp` here and then try to connect, you can see that we do indeed establish a connection. And even better, we can see our three tools.

If I do the `get_alerts` tool and specify `MN` for the state as an example, when I call the tool, it works. And if we go back to the Aspire dashboard and look at the **Traces** tab, we can see the trace for our last call, which spent most of its time in the remote weather.gov API endpoint.

Let's update the code to fix our initial connection issue and then try the other tools. If we hover over `WithMcpServer`, you can see that there's an optional `path` argument that defaults to `/mcp`. So let's just set that explicitly to an empty string. In case you're curious, the other way we could have resolved that and left the default path alone is to modify our call to `MapMcp` in `Program.cs` of our MCP server — if we mapped that to `mcp`, then the app host code would work with that default of `/mcp`.

Let's run it again. Here's the dashboard, and let's hit our client. Sure enough, no `mcp` on the URL this time. And when we hit **Connect**, it works. Let's try our `get_simple_weather` tool. It works too — there's the forecast. And in the traces in the dashboard, we get more detail this time since the weather forecast code is ours. Just for completeness, you can put in some lat and long values for this final method, and it should also work. Feel free to explore logs, traces, and other such items in the Aspire dashboard. We'll be doing a lot more of this in the course as well.

### Our Application — with No AI or MCP

We just did two demos to create some Hello World-style MCP servers, and we're ready to start making this a bit more real. We'll be focused on adding an MCP server to enable AI functionality for a company called **Carved Rock Fitness**. This is a fictional company that sells outdoor recreational fitness equipment. They want to add AI features to their UI.

The README has a listing of the various features already in the application, none of which include AI or MCP. But it's an e-commerce type application with some admin capabilities. It's super simple and not intended to be a real production e-commerce application, just complex enough for us to build our understanding of how to implement MCP servers properly. We'll be setting up an MCP server that can make tool calls, and those may be API calls or other such custom code. We'll need to make sure we address security concerns along the way since we'll have some logged-in users, as well as some administrators. Our goal here is not the best AI nor the best e-commerce app, but rather to learn about the features of MCP and how to use the C# SDK to enable them in your own applications.

Let's look at our starting app. Here's the README with a rundown of the features. In the Solution Explorer, you can see the projects. The top-level projects are the **API** and the **web host** (the UI), and then there's the **Aspire app host**, and the rest of the projects are class libraries.

The easiest way to show you the features, which are very simple, is to run the app. It should just be a matter of hitting **F5**, but you may need to make sure that your app host is your startup project. Here's the dashboard coming up. If you haven't pulled the Postgres database image, that may mean your database takes a little longer to start up. Once all of the apps are running, let's hit the link for the API. The main thing you can see is that what we have at this point is primary **CRUD-type operations** for products: create, read, update, and delete. If you try out this first GET operation, then hit **Execute**, you should see a JSON response with a list of products.

Let's go back to the dashboard and hit the UI. If we hit the **Footwear** link, you can see a list of footwear products. The images won't match or make sense, and that's because for this list, I'm just using a service called pixum.photos to have random images for each item. If we add an item to the cart, the cart total gets updated. When we go to the cart, you need to log in, and that's done via a public demo instance of **Duende IdentityServer**. The logins you can use are on the page, or you can use your Google account. I'll use `bob` / `bob`. When I signed in, notice the new **Admin** link that showed up — we'll come back to that.

Once you're in the cart page, you can go to a checkout page where presumably you would enter payment and address information. For now, you just click **Submit Order** and it's done. A thank-you page is displayed that says a fake email was sent. If you go back to the dashboard and hit the link for the SMTP service, it takes you to a **Mailpit** UI where you can see the email that was sent. Nothing fancy, but it works.

Since I was logged in as Bob, the admin link takes you to a page where you can manage the list of products. If we edit a product and change its price from `74.99` to `84.99`, then click **Save**, we can see the updated product in the list. All of the traces and logs go into the Aspire dashboard, which you can explore on your own. But with this overview completed and our starter MCP servers also done, we should now be able to start implementing an MCP server for this application.

## Creating an MCP Server for APIs

### Introduction

In the last module, we introduced the C# MCP SDK. Now, we're ready to get started creating the MCP server for our Carved Rock application. This module will be super demo-heavy, so let's see what we've been asked to build.

We need to do our part to accommodate a feature request to provide recommendations in a chat interface using AI. The user would input some text indicating what they're looking for, and then AI would use that input along with the products in the Carved Rock catalog to make some simple recommendations. Our first task will be to create an MCP server with a tool that can return all of the products. We also need to create automated tests to ensure that our MCP server is making the expected tool available and that it works like it should. So let's jump in.

### Demo: Add an MCP Server with a Tool

We're looking at the Carved Rock solution, and this is the app host that shows the orchestration logic for the project. It doesn't have an MCP server or the MCP inspector for us to manually interact with our server. The three things we'll be doing here are:

1. Add an MCP server using the ASP.NET Core MCP SDK.
2. Add the MCP Inspector to the orchestration so we've got a way to interact with our server.
3. Add a tool that calls the `GET /products` API, run it, and make sure it works.

Pause the video if you want to practice and then see how closely what you did matches up with what I do. Okay, let's go.

The first thing is to add a project for the MCP server itself: **Add a new project → ASP.NET Core Empty** template. I'll give it a name of `CarvedRock.Mcp` and click **Next**, leaving the defaults which will enlist it in the Aspire orchestration, then click **Create**. That gives us a new line in the Aspire app host for our new MCP server. I'll capture the return of this line into a variable called `mcp`. Then I'll simplify the name that will show up in the Aspire dashboard. Then I'll add a reference to the API since we'll be calling that from our MCP server, and I'll add an HTTP health check reference. I'll add the health check refs to our other projects here as well — this will make it easier to reference those health checks after deploying to higher environments.

Now let's look at `Program.cs` for our MCP project. We're going to need to add MCP into the service registration in the pipeline, so let's add the required NuGet package. Prerelease is checked, so I'll search for `ModelContextProtocol`, choose the **ASP.NET Core** version, and install it. And since we're dealing with NuGet packages, let's add the MCP Inspector to the app host. I'll right-click the app host project and choose **Manage NuGet Packages**. If I search for `Aspire MCP`, there is the package that contains the MCP Inspector. So I'll install that and accept the terms.

The changes to our app host code are minimal, so let's get those out of the way just by pasting in some code. It adds the MCP Inspector to the orchestration, then refers to the MCP server that we captured in that variable a minute ago. The path is set to empty here since we want the MCP content to be served from the root path of our MCP server.

Let's go finish our MCP server and add the tool to call our products API. We've got the correct NuGet package, so after the `AddServiceDefaults` call, I'll do `builder.Services.AddMcpServer` and then specify the **HTTP transport** for it. I'll leave the tool registration for now, but let's add a named `HttpClient` for our API. The code being suggested here is almost correct, but I need to change the URI to `HTTPS API` to match what's being made available within our app host. Then, after `MapDefaultEndpoints`, we can replace the `MapGet` for a Hello World string into `MapMcp`. And we don't want to specify any path, so this is fine as is.

Now let's add the tool that we'll call our API. **Shift+F2** on the project node to add a file, and I'll call it `CarvedRockTools.cs`. I'll add the `McpServerToolType` attribute to this class. Then I'll paste in a simple method and resolve the issues. The method is called `GetAllProductsAsync`, and it calls our `GetProduct` endpoint in the API with no parameters.

- The first issue to resolve is that it needs an `HttpClient` factory, so let's add a primary constructor and inject one. But it's an `IHttpClientFactory`, so I'll fix that.
- The next issue is that it doesn't know what a product model is. It's a class that I've got to find in the core library. I'll grab that definition and create a new file called `ProductModel.cs` in the MCP assembly. For this example, I want everything except the image URL — the AI won't have any need for that, so I'm using a custom record in this MCP server that provides exactly the shaped object that we think we'll need.

This new method is an async one that includes a cancellation token as an input and creates an `HttpClient` for our API using the injected factory. Then it makes a call to the API and deserializes the response into a list of product models and returns it, or an empty list. Let's add the `McpServerTool` attribute here, provide a name of `get_products`, and then a description. The `Description` attribute needs a using statement, and we're good.

You might be tempted to run it now, but there's one more thing we need to do — we need to register the tool somehow in the services collection. In `Program.cs`, after we added the MCP server, we can add a call to `WithTools` and add a reference of our class `CarvedRockTools`. Now let's run it. We should see our MCP server and an inspector in the dashboard, and they're both there. Let's hit the Inspector Client link. It opens fine, with our MCP server referenced in the URL. I'll hit **Connect**, and that works fine too. Now list the tools and there's our `get_products` tool. Let's try it — it works too. We're off to a good start.

### Options for Adding Tools

It's pretty likely that the most common thing you'll be adding to an MCP server is tools. An important concept, at least for now, is that **if you make too many tools available to an AI service, it can become confused** and either call the wrong tool or not make a tool call at all. So getting only the right tools added is important, and knowing what your options are to add them is a key part of that.

1. **`WithToolsFromAssembly`** — we saw this in the first module. If you call it without a parameter, it'll default to the calling assembly, but you can provide an assembly name that contains the tools you want to add. It'll use reflection against that assembly and look for the `McpServerToolType` classes, and within those classes, look for methods decorated with the `McpServerTool` attribute.
2. **`WithTools<Type>`** — what we just did in the previous clip. You specify the type, and it'll look for methods in the provided class that have the `McpServerTool` attribute on them. The class-level attribute is actually not required here.
3. **`WithTools` with a list** — call an overload of `WithTools` and not provide a generic type but a list or `IEnumerable` of tool methods, and each of the methods needs to have the `McpServerTool` attribute.
4. **`ConfigureSessionOptions`** — a more advanced option where you can dynamically create a list of tools via the `ConfigureSessionOptions` method on the MCP server. You might do this to make certain methods available based on an authorization context, for example, and we'll see this in the next module.

And you can also combine these options. Just keep in mind that too many tools can confuse an LLM.

### Demo: Reviewing Requests and Responses to MCP Servers

Now let's have a closer look at the actual requests and responses to our HTTP-based MCP server. I've got the same solution running, and here's the Aspire dashboard. Let's go back to the MCP Inspector and click **Connect**. That works just like it did before, and I'll click the **List Tools** button, which also works. Both of these actions — connecting to the server and listing the tools — are actually HTTP requests, and we can inspect them in the **History** area of this page.

When I clicked Connect, it made the `initialize` request. The request sent a JSON payload with an object that has a `method` property set to `initialize`. And the response that was sent back is some JSON that includes **capabilities** and some server information. Capabilities includes a node for `tools` that indicates that the available tools have changed. Nothing too crazy, but good to be aware of these things — it'll help our understanding when we start talking about security in the next module.

Let's look at this `tools/list` call. It's a simple enough request, similar to the `initialize` one, with a `method` property set to `tools/list` and an empty `params` object. Then the response is an object with a `tools` property, which is an array of tool definitions that includes a name, a description, and an object to describe the input schema for calling the tool. In this case, it's an object with no properties — the `get_products` method doesn't take any parameters.

Let's call this tool. I'll hit the **Run** button with the tool selected. It works, and we can see some content in the result pane. Let's expand this new history item. There's a `method` property again, set to `tools/call`, which means we want to invoke a tool. The name of the tool is in the `params` — it's `get_products` — and there aren't any arguments. Then the response is an object that includes a `content` array, and the first item in the array has a `type` of `text`, and the `text` property is the actual JSON content from our API call.

If you're wondering why the requests and responses are formatted this way, it's actually using the **JSON-RPC 2.0** standard, and both the client and server SDKs just make it easier to work with that. In this History tab, the content within this response block is truncated, so you won't see the entire response here, but you can in this **Tool Result** section.

So that we can see some of this more clearly, let's pop over to Postman and make this same tool call. I've got the same connection made to our running MCP server, and our `GetProducts` tool is listed. When I click **Run**, it works and shows our content. The content is an array with one item, and that item is `text`, and the text is a single string of JSON with escaped double-quote characters. The **Preview** tab shows the content in a more easy-to-read format.

Note that this JSON-RPC response format is **not the same** thing returned by the API. If we pop back to our Aspire dashboard and hit the link for the API, we see the Swagger UI. If I expand the first method, try it out, and hit **Execute**, that result is a simple JSON array with no wrappers for the JSON-RPC. All of this is simply to say that the responses that you provide from an MCP server will not directly match what you return from an API, and that's by design.

### Demo: Logging and Tracing for our MCP Server

One of the important things to understand both during development and pushing forward into higher environments is logging and telemetry. Here's our single tool method so far, and let's say we wanted to add some log messages from inside this method. Since this is an ASP.NET Core application, we can simply follow the common pattern of injecting an `ILogger` of the type of our class, `CarvedRockTools`, into the primary constructor. Then in the method, we can log a message. This code should look pretty familiar if you've done other ASP.NET Core development.

Let's run the application. I'll go into the inspector, connect to our MCP server, list the tools, and invoke the `get_products` tool. It works fine, no surprise. But let's go check the logs and traces in the Aspire dashboard. If we go to the **Traces**, this last trace is the one for the tool call. You'll notice that all the calls to our MCP server are **POSTs** — that's JSON-RPC, a lot like SOAP if you've ever worked with that. Here's the trace, and you can see the different activity that was involved in this tool call: the MCP server makes a remote call to the API, the API starts processing the request and makes a database call to get the data. Each of the activities here has more information if you click on them. And in the **Logs** tab, you can see our new log entry from the MCP server where we fetched 50 products.

But there's even more power here in Aspire, which has been improving and changing very rapidly. You can see at the top of this page that there's a new version we can upgrade to. Let's stop this, and then from the solution item in the explorer, let's **Manage NuGet Packages for the solution**. I'll go to the **Updates** tab, uncheck the prerelease item, then select all packages and unselect AutoMapper. (AutoMapper isn't actually used in the solution, and I'll remove it later.) I'm updating to **Aspire 9.5.1** and some other packages. I'll accept the terms, and the upgrades are done.

Let's run the solution again and do the same thing in the inspector — Connect, list the tools, and run `get_products`. Now let's look at the traces again. It looks almost the same as before, but we also now have some **dots** in the lines for the traces. These are actual log entries from the different services that are correlated to exactly when they occurred during the trace and the service that they occurred in. In this orangeish trace for the API, we can see a log entry about getting into the repository code. And if I look in our MCP server, sure enough, there's our entry about fetching the 50 products.

This is really powerful information to have during development, and it's all based on **OpenTelemetry**. The logging code we added was just to create a log entry, but the fact that all of this goodness shows up in the Aspire dashboard isn't magic. If we go back to the `Program.cs` file of our new MCP server, there's this line to add service defaults. This is an extension method in the **ServiceDefaults** class library. Here is that method — the `ConfigureOpenTelemetry` method — configuring OpenTelemetry, adding some health checks and more. It does message formatting, includes scopes, and then adds metrics and tracing.

If you want to learn more about this service defaults approach and how to customize and use the OpenTelemetry stuff, check out another course I did: *.NET Cloud-native Development: Working with Docker and Aspire*. The rest of this service defaults code is much of it from the default version of this project from the Aspire team, but a little bit has been customized by me, and this is all code that you can make your own. For now, just know that logging is important and using these techniques that Aspire provides is super helpful.

### Question: What Logic Belongs in a Tool?

Before we move on to writing tests, I wanted to pause for just a minute and address the question of **what logic should we put in an MCP server**, and why not just configure tools in our agent code to call APIs directly? The tool method we've already created is a very simple one that calls an API and returns the payload with one field omitted. But there are any number of things you might want to include in the logic of an MCP tool method:

- **Cache** the results of an API call if it's a longer or heavier call with response content that is a good candidate for caching.
- Add some **computed fields** into the mix. Remember that an LLM is not typically good at discrete calculations, but code is. Adding some computed content can help give better results.
- Further **shape content** than the simple example I've shown. Maybe you've got complex JSON from the API that can be simplified for the MCP response — fewer fields, a flatter graph, or other such changes.
- **Combine more than one API call** to perform a single logical operation without needing multiple tool calls.
- Provide a **simpler way to call an existing API** for a common use case (we'll see an example of this in a few minutes).
- Your MCP tools absolutely **don't need to be confined to making API calls**. The whole .NET world is available to you. You can create your own custom logic with whatever makes sense, and this includes AI agent code that could call a gen AI model itself. This technique is referred to as **agent as tool**.

Just remember through all of this that the content that you return from an MCP tool should be crafted to help an LLM or AI service easily make sense of the content for its own use. And this goes for the input parameters to your MCP tools as well. What I'm showing here are some simple examples that can't possibly touch on everything you could put in a tool. It'll be up to you to create the tools that make the most sense for your AI-based applications.

### Demo: Set up an Integration Test

One of our tasks for even this first feature is to create automated tests for our new MCP server. Since the logic of the MCP tool call that we have is to call an API method, we probably want an **integration test** for this rather than a unit test. I'd rather see the whole thing working than spend time mocking an API. Fortunately, the Aspire approach to development and testing is there to help us.

I'll create a solution folder called `tests`. Then I'll right-click on the new folder and add a new project. For this project, I'll choose the **.NET Aspire Test Project**, and I'll use the **xUnit** flavor. I'll call the project `CarvedRock.IntegrationTests`, and I'll specify a folder location by creating a new subfolder called `tests` to match the solution folder. This way the Visual Studio Solution Explorer view will match the folder structure in the repo. I'll click **Next**. I don't have the option to update beyond Aspire 9.3 here, and we just updated to 9.5.1 in the last demo — we'll address that shortly. I'll also choose the **v3** unit of xUnit, then click **Create**.

When you do this, the package dependencies will show a warning icon. If we look at the `.csproj` file, you'll see that the five referenced packages are all showing version numbers. This solution is using **central package management**, and we can fix these errors by moving these version numbers into the `Directory.Packages.props` file in the Solution Items folder. I'll copy these references, then open that props file, paste the references at the bottom, and update the `PackageReference` node to be `PackageVersion`. I'll fix the spacing and save this file. Then I'll go back to the `.csproj` file for our new test project and remove the version numbers. Finally, to make sure we're up to date, I'll right-click this project and manage the NuGet packages, and update them all.

Now let's take a look at this `IntegrationTest1.cs` file. It's basically all commented out, but there are some instructions:

1. Add a project reference to the app host project. I'll right-click the project → **Add → Project Reference**, then choose the app host project.
2. Comment in this `Fact` method.

I'll get rid of the comments now. Let's have a look at this code:

- We set up a **cancellation token** to gracefully handle it if and when we cancel test runs.
- There's this fairly magical-looking line that creates a **distributed application testing builder** with our app host project. That basically creates a builder for our entire Aspire-orchestrated solution. Since it's a testing builder, it's using different ports and won't include the dashboard or launch a browser since we're just running tests.
- There's a block that configures logging and points us to some docs if we want to use the `ITestOutputHelper` of xUnit.
- There's a block to configure some resilience handling for an `HttpClient`.
- Then the app host builder is built into an application and the application is started.

The **act** part of the test gets an `HttpClient` that would reference our front end, but our front end doesn't use this name. Let's look at the app host — we call our UI project `webapp`. I'll copy this value. So we create an `HttpClient` for the web app, then wait for it to be healthy for the default timeout (set to 30 seconds in the code above), and then it does a `GetAsync` on the root URL. It asserts that we get an OK response. I'll update this method name to `GetHomePageReturnsOkStatusCode`. I'll also update the file name to be `WebAppTests.cs`.

Let's run this test. I'll open the Test Explorer and click **Run**. When it finishes, the test has passed. We want to test our MCP server though, and now we should have everything in place to do it. Let's do that next.

### Demo: Add a Test for Our MCP Server

Now that we've got a basic integration test for the home page, let's get a test class started that we can use to test our new MCP server. I'll copy all of the code we just created for the web app test, create a new class file called `McpServerTests.cs`, paste in that code, rename the class back to `McpServerTests`, and start making changes.

The first test we'll create will be to make sure that the `ListTools` call will include the `get_products` tool in the list of tools that this MCP server provides. We're going to be using the **client portion** of the MCP SDK, so let's manage the NuGet packages for this test project and add a reference to `ModelContextProtocol.AspNetCore` (still a prerelease package). Accept the terms.

We can leave everything in place through the `app.StartAsync` call since that is what's wiring up and starting our complete Aspire application. But I'll completely get rid of the **act** portion of the test. What we need to do is use the client portion of the MCP SDK and get an MCP client:

1. I'll set up a variable and set it equal to `McpClient.CreateAsync`, and this will need to be awaited. We're going to need to provide some arguments — the real required arg is an `IClientTransport` object.
2. I'll create a new variable for a `ClientTransport` object and set it equal to an `HttpClientTransportOptions` instance since our MCP server uses HTTP as its transport protocol. Then I'll set the endpoint and set the transport mode to `StreamableHttp`.
3. Because we have the full Aspire application here, it's really easy for us to get the URI for our MCP server. I'll set the endpoint to `app.GetEndpoint` for our MCP server, referencing the HTTP endpoint. (Even simpler, I don't need a variable for it, but can call that `app.GetEndpoint` method right there in the initializer logic.)
4. Then I can call the `CreateAsync` method with a new `HttpClientTransport`, pass our options object to it, and include a cancellation token.

With our MCP client now available, we can call the `ListTools` method on it and include the `cancellationToken` again. Then for our actual test, we can make sure that the response includes a tool called `GetProducts`.

Let's put a breakpoint here and debug this new test. If we look at this `clientTransport` variable, we can see that it's got a real-looking endpoint set to a different port than when we ran the application before, but it looks good. And the transport is `StreamableHttp`. I can step along, and when I make the `ListToolsAsync` call, we can see that the response object has one item in it. Expanding it, it indeed is a tool called `get_products`. I'll continue, and then I see this error of a task canceled — this is actually nothing to worry about and didn't affect our tests, which passed.

You may be thinking that we've got a lot of duplicated code at this point, and we're setting up the entire Aspire application in each test. If this occurred to you, that's awesome. We'll fix it in the next clip before we add a test that actually calls the `get_products` tool.

### Demo: Simplify the Tests (and Homework!)

The two tests we created have duplicated the logic to set up the Aspire distributed testing application builder, which would add a lot of overhead to each test. Fortunately, most test frameworks have a way to add some startup logic either to test classes or to the overall test project itself. Here's the documentation for xUnit:

- For logic shared for all tests in a single class, we can use a **class fixture**.
- A **collection fixture** can be used for all tests in a project (you can explore that on your own).

We need a fixture class that'll contain our distributed testing application, and then our test class will implement `IClassFixture<T>` of that type and inject a fixture into it. Let's give it a shot.

I'll use **Shift+F2** to add a new file, specify a new `utils` folder, and name the class file `AppFixture.cs`. I'll make the class public, then specify the `IDisposable` interface, and accept the suggestion for the starter constructor and dispose methods. Now I'm going to paste in a fair bit of code:

- A private variable called `defaultTimeout` based on the starter code we had.
- A **JSON serializer** object with camel casing that we can use when deserializing content from MCP tool calls or API calls.
- A `CancelToken` that we can make available, a `DistributedApplication`, and an `McpClient`.

Our job in the constructor is to make sure all of these properties get set. Let's look at the constructor: it sets the `CancelToken`, which is fine, but then it calls this missing method called `InitializeAsync`. The logic to create and start the application is async, so we'll need to call an async method and then make sure we wait for it to complete during this constructor. I'll add some cleanup logic in the dispose method.

Let's get the `InitializeAsync` method created. This is actually the same startup logic that we had in the test methods earlier: we create a distributed application testing builder, configure some `HttpClient` defaults, then build the application and start it. That assigns the `app` that we can reference from anywhere the fixture is injected. And next, we have some logic to set up an `HttpClientTransportOptions` variable with the MCP endpoint using `StreamableHttp`, and we assign the `McpClient` property to the `CreateAsync` method.

Let's update the `McpServerTests` class to use this new class fixture. I'll specify an interface of `IClassFixture<AppFixture>` and add a primary constructor that will inject an `AppFixture`. Then I should be able to comment out all of the code that sets up our `McpClient` — we can simply reference the `McpClient` from the injected fixture. I'll also use the `CancelToken` from the fixture. Now our test is much more readable: use the `McpClient`, list the tools, and make sure that `get_products` is one of the available tools.

Let's run this test and make sure it still passes. And it does. I'll leave it to you to update the web app tests to use this same app fixture approach. I also encourage you to add another test to make a tool call and confirm results. I've added that and some other tests that you can review in the exercise files and repo, but this approach should unleash you to create powerful and efficient tests on your own.

## Security Considerations for MCP Servers

### Key Security Questions and OAuth

We've gotten the basics of our MCP server working, and now we're ready to get it more production-ready. If you're doing anything with APIs and ASP.NET Core and want to expose some functionality via MCP, you'll definitely want to pay attention to security.

I want to pose a couple of security questions that you should think about:

1. **Should connecting to your MCP server be allowed for an anonymous user?** Connecting to your server with the `initialize` request and then listing tools or other resources may be operations you want to protect, or maybe you want to leave those open.
2. **Should each tool be secured** and require some kind of authenticated user or connection? Each of these questions applies to tools and other features of MCP servers like prompts and resources too.
3. **Do some of the tools require different permissions?** If all of the tools should be secured and require at least an authenticated connection, you may want your entire MCP server to be secured.

To handle security for MCP servers, **OAuth** is a great and first-class way to address the requirements. The official ModelContextProtocol docs have a lot of information you can review on your own, including a more detailed link to the OAuth spec for MCP. If we go down this page a little, you'll see a mermaid sequence diagram for **metadata discovery**, and a little further below, a diagram for the entire **authorization flow**. You'll see all of this in action in just a few minutes.

### Security Scenarios and Our Approach

When dealing with an MCP server and the security for it, there are four scenarios to consider:

1. **Anonymous access** — basically no security. This may be appropriate for some use cases, and we've already seen that implemented on our `get_products` tool.
2. **Machine-to-machine** — something like an AI service or agent doing background work on a schedule, but not representing a logged-in user. The OAuth concept we'd embrace here is **client credentials**: the AI agent would have its own client ID and some kind of secret like a password, and it could get its own tokens to authenticate on our MCP server.
3. **User-based access** — a person is logged in and can use our MCP server through an agent. Simply logging in may be enough, but sometimes more granular permissions are needed, and that's where we can use role-based, policy-based, or even custom access control. This OAuth flavor is called **interactive**, and it generally involves a user logging in and getting a token that's passed along with calls to the MCP server.
4. **Delegated access** — you may want awareness that an AI agent is acting on behalf of a logged-in user. Here a token is retrieved by means of **token exchange**.

ASP.NET Core has some great built-in features for using OAuth to handle security concerns, and they are all available to us in the context of our MCP server. Here's a review of what we're about to do:

- Get OAuth enabled for our MCP server, using the MCP Inspector to support our authentication flow.
- Make sure the tokens we use to authenticate on the MCP server can be **forwarded to the API** we're calling, which has its own security requirements.
- Add more sophisticated **logging and telemetry** that includes details about the logged-in user and the MCP interactions.
- Make sure we have appropriate **role-based access** for both calling tools and even listing them. We don't want non-admin users to be able to see tools that should only be available to admin users. (The admin-only tools are to **delete a product** and **update a product price**, which I created on my own. They'll fail until we add bearer tokens to the API calls.)
- Wrap things up by creating **automated tests** to verify all of our authentication and authorization logic.

### Demo: Enable OAuth for Our MCP Server

Our first step is to get OAuth enabled for our MCP server. I've added some new code after the call to `AddServiceDefaults` in `Program.cs` of our MCP server. Before we look at this, let's look at the code I've referenced in the comment — the `Program.cs` file from the `ProtectedMcpServer` sample in the C# MCP SDK repo:

- The `CreateBuilder` line is there, setting some variables to the server URL for this MCP server, and a URL for an authentication server (in this case, an in-memory one).
- There are blocks to add authentication and a **JWT bearer** option. The valid issuer for a JWT is the authentication server from the in-memory URL. It's got some event handlers for jobs that'll do some logging, but those aren't explicitly necessary.
- There's a block for `AddMcp` on this authentication block — this is new and important. We provide some **resource metadata**: the resource is the URI for the MCP server we're protecting, documentation if we have it, a list of our **authorization servers**, and the **scopes** that we can support.
- There's a line for adding authorization to the services.
- Below that, `AddMcpServer` adds some tools using the same HTTP transport.
- After `builder.Build`, we see `MapMcp` with a `RequireAuthorization` extension on it, which will require authentication even when connecting to the MCP server.

Let's go see the code that I've added to our own MCP server:

- I've got variables for the `AuthServer` URL and the `McpServerUrl`, but instead of hard coding them, I'm reading them from configuration. `appsettings` has that demo Duende IdentityServer for our OAuth server and the URL for our local MCP server.
- `AddAuthentication` and `AddJwtBearer` just like in the sample, without those logging event handlers. The issuer is our auth server.
- The `AddMcp` block references our MCP URL and the `AuthServer` URL, with some different scopes listed (just what the demo identity server already supports). In real life, you'd set up whatever scopes you wanted.
- The line to add authorization.
- The code to add the MCP server, and I've added some options to the HTTP transport setup — I'm making this **stateless**, which is important if you intend to deploy your MCP server into some kind of load-balanced environment.
- After `builder.Build`, lines that add authentication and authorization to the pipeline, and we're requiring authorization on the mapped MCP content.
- I added a NuGet package reference for `Microsoft.AspNetCore.Authentication.JwtBearer`.

Let's try it out. Here's our dashboard, and I'll go into the MCP Inspector and try **Connect**. Wow, that didn't work. It's trying to go to an authorize URL, and this `5241` port is what our MCP server is using — so it's not using that demo identity server reference at all. One of the handy things about the inspector is that it can help you see where the issue might lie in that flow.

I'll click **Open Auth Settings**, then the **Quick OAuth Flow** button, which gives an error about failing to get OAuth metadata. There's an **OAuth flow progress** section — I'll click **Continue** to try the first step, and it fails with a similar message. Let's open the browser tools for another clue. The **Network** tab is showing a bunch of red — if you're seeing these **preflight** entries, you might be getting a stronger suspicion of what's wrong. The **Console** tab is showing a bunch of **CORS failures**. The MCP Inspector is initiating interactions with our MCP server from the browser, and it's running on a different port. So we'll need to enable **CORS**.

I'm going to copy in some code for an allow-anything policy called `DevAll`. Then, right after `builder.Build`, I'll use the CORS policy of `DevAll`, but only if the environment is development. You're going to make your own choices here about whether browser-based apps should be able to call your MCP server. This allow-everything policy is almost certainly not what you would want to use in production.

Let's try this again. Click **Continue**, and this time, it worked. If we expand this section, we can see that it found some **resource metadata** at this well-known OAuth protected resource endpoint — these are the values that we specified in that `AddMcp` block. The next doc down is from the demo Duende IdentityServer.

I'll click **Continue** on Client Registration, and that works too, but I'm thinking it probably didn't work for you. The demo identity server isn't configured to support **dynamic client registration (DCR)**. Duende IdentityServer does support it, but you have to configure it based on your rules, and their public demo instance doesn't have it. I've got this client ID in the Authentication section of the inspector set to `interactive.public`. If I remove this, clear the OAuth state, and hit Continue to do step 1 again, that works. And continuing again, I get an error on the client registration setup.

If we go to the demo identity server instance, there's a tab where you can see the clients it supports, and we need an **interactive** one since we're coming from the browser. Browsers can't keep secrets, so I picked the `interactive.public` one. Let's plug that back in and keep going. Hit **Continue**, and now it's working again. Hit Continue again, and this time, we get a long URL hitting the authorize endpoint for the identity server. Let's click this link to go to the URL.

Okay, so we got to the identity server, but then got an error about an **invalid target**. If you look closer at this authorize URL, it all looks pretty good, but at the end, there's this `resource` item that references our MCP server, and that seems like it might be the problem. Let's figure out how to get this resolved and logged in the next clip.

### Demo: Getting OAuth Working for Our MCP Server

To get our OAuth working, I needed to include the demo identity server code in our solution and make some minor changes. In real life, you'd have your own identity server, whether that's something like the one from Duende or Auth0 or some other provider. Here's what I did:

- Cloned the GitHub repo, copied the project into the folder for our solution, added the project to the solution, and added a reference to it in the app host project.
- Updated the NuGet references in the identity server project to use central package management, and updated their package versions to the latest.
- Added calls for `AddServiceDefaults` and `MapDefaultEndpoints` to the identity server code, and changed the logging logic to only use what's provided by `AddServiceDefaults` so that I could see everything in the Aspire dashboard.
- Added a line to include the identity server when our Aspire solution starts up.
- To make our MCP server use this identity server as its OAuth server, I'm overriding the `appsettings` value with an environment variable in the app host, pointing to the HTTPS endpoint for the local identity server.

Let's try this. Here's the dashboard with our identity server running locally on port `5001` for HTTPS. Let's go to our MCP Inspector. The Authentication section is still showing `interactive.public` as the client ID. Let's go to the OAuth flow helper and click **Continue**. Metadata discovery works, and looking closer shows that we are indeed referencing our identity server locally on port `5001`. Client registration works, and I'll continue again. We see that authorization URL, almost the same as before, but for our local identity server. Let's click this link.

It still shows **invalid target**. But now, since we're running locally, we also see this message about an **invalid resource indicator**. That's helpful. If we go back to the dashboard and click the error icon by the identity server, we go straight to those errors — it's telling us that `localhost:5241` is not found in our resources. Let's fix that with a configuration change to our local identity server.

In the identity server project, there's a file called `HostingExtensions` which has all of the configuration for identity server itself, and it has a call that adds **API scopes** / **API resources**. There's a list of API resources being created. I'll paste in a pretty close duplicate of the first API resource. The only difference is the API resource constructor — the URL is the first arg for the name of the resource, and then I've got a description for our local MCP server.

Now let's try again. I'll go straight to the progress area and hit Continue, Metadata, Client Registration, and Preparing Authorization. Now for this authorize link — and this time, we get a **login page**. I'll log in as **Bob**, then agree to this consent page. Then I'm presented with a page back on our MCP Inspector URL (note the `6274` port), and I can grab this whole code value. Then I'll go back to my main page and paste this into the authorization code form field, then hit **Continue**. That completes both of the last steps, and we're good.

So now when I hit **Connect**, it works, and we can list the tools. Click the `get_products` tool, and running it works fine too. I'll try `delete`, product `3`, and this not surprisingly failed and is showing an exception of a **401** response code — we're still not passing any tokens to the API when we call it.

One other thing I wanted to show you is how simple the auth is now that we've gotten the setup right. I'll disconnect, use this auth link and clear the OAuth state, then click **Connect**. That shows us the consent page, but since I was already signed in on the browser, I didn't have to sign in again. That step-by-step flow was only to help us get things set up correctly. Now, you can just hit **Connect**, and you'll go through whatever is needed and be connected.

### Demo: Forward Tokens for API Requests

Now that we've gotten OAuth working, let's make sure we can forward any auth tokens to the APIs that we call. I'll create a new class file called `TokenForwarder.cs`. It gets an `IHttpContextAccessor` through dependency injection in the primary constructor, and it inherits from `DelegatingHandler`, which allows an override to the `SendAsync` method. We look at the `HttpContext`'s incoming request, and if that has an authorization header that's a bearer token, we add a new authorization header to the request being sent and then send it. Pretty straightforward.

Now, let's add this handler onto the `HttpClient` for the API. In `Program.cs`, the forwarder needs an `IHttpContextAccessor`, so we need to add that to the services. Then we can add a transient instance of our token forwarder (so we'll create a new one with each request), and finally add an HTTP message handler onto this client.

Let's try this out. Dashboard, MCP Inspector, **Connect**. I'm already signed in as Bob, so I'll agree to this consent. I'm connected and can list the tools. Choose the `delete` tool, try an ID `4`, and click **Run**. We're still getting a **401**. Let's check the API logs. It looks like the bearer token validation failed — we're sending a token issued by `localhost:5001`, and the API is expecting one from `demo.duendesoftware.com`. That makes sense since we haven't changed the API. Our token was forwarded, but the API was expecting it to come from somewhere else.

The API defines the authority in its `appsettings` file — it's this `Auth:Authority` item. We can override that with an environment variable in the app host. I'll copy the line where we do that for the MCP server and paste it into the builder for the API, and the environment variable is `Auth__Authority` to match what was in `appsettings`.

Let's try again. Hit the inspector, **Connect**, and grant consent. List the tools, `delete`, and try `5`. Run it — it worked. Let's look at the trace quick. We don't see mention of a tool call, just a POST to our MCP server, and then it makes a DELETE HTTP request. We can see this log entry shows the DELETE request made by the MCP server and the path, but we don't see any information about the user that did it here. These orange lines are from the API — if I click on one of those, we can see the request path, and even better, we can see the **logged-in user**. We'll want to get some of this wired up for our MCP server to support better troubleshooting. But our token is being forwarded, and our API is accepting it.

### Demo: Improve Logs and Telemetry

It's super important to have good logging and telemetry in your production application, and an MCP server is no exception. As new as it is, it may even be more important. We saw that the API logs included the user represented by a bearer token, and we should include that information for our MCP server logs when we have an authenticated user. This is in place for both the web app and the API already, done by a custom piece of middleware.

One of the things I like to do is have the **email address** included as part of the log entry as the name of the user. To do that, we can add a setting into our token validation parameters that says the **name claim type** will be the **email claim**. This will set the name property on the user for the HTTP context with the value of the email claim from the identity provider.

Now, let's look at that middleware — it's called `UserScopeMiddleware` (there's nothing special about the name). It looks at the HTTP context for the user property. If it's authenticated, it'll grab a couple of values from the claims of the user and call `await next` within a **logging scope** that includes those claims. If you had more or different claims than these, you can easily customize this code. With this middleware already defined, we just need to add it to our MCP server, right after our calls to `UseAuthentication` and `UseAuthorization` — and that needs a project reference to our core project.

Now let's look at the logging that gets set up in the `AddServiceDefaults` calls — the `ConfigureOpenTelemetry` method. In the logging section, there's a line to **include scopes**, which is important for our `UserScopeMiddleware`. We're going to add two items here now:

- In **metrics**, a meter for `Experimental.ModelContextProtocol`. (If you want to make sure everything that publishes OTel metrics gets included, or this experimental value changes and you can't find your MCP metrics, you can use the asterisk `*` as a wildcard, review the entries, and add back exactly what you want.)
- For **traces**, a source with the same value.

With just those changes, let's run this again. Inspector → Connect → grant consent → list tools. Choose `get_products`, then run the tool — that works. Choose `delete`, try `7`, run the tool. Now, let's check the traces. Now we can easily see that this is a tool call to the `get_products` tool — this came from adding that trace source. And if I click this dot for a log entry in the MCP server, you can see the user's email and user ID. This is super powerful stuff, but we're still not done. We need to do a better job of controlling which tools users can see and invoke. We'll do that next.

### Demo: Implement Role-based Access for Tools

When you get an access token from an identity provider, it'll have a set of claims about the user, and sometimes those claims will include roles or other things you need to properly authorize the user. But in our case, the demo identity server we're using is not sending any role claims about the user. The API and web app already use this technique — you can add what's called **claims transformation** with an implementation of the `IClaimsTransformation` interface to the Services collection, and we've already got an `AdminClaimsTransformer`.

It implements `IClaimsTransformation`, which has a single method called `TransformAsync`. The whole point is that you can look at a claims principal (the one defined by decoding the incoming JWT) and then return a new principal based on that one. The super contrived logic here is that if there is an email claim and the name part of the email is `Bob Smith`, then a new principal is cloned from the incoming one, an admin role claim is added, and this new principal is returned. Otherwise, we just return the original unchanged principal. You would probably find the user ID, look them up in a database, and add claims based on what comes back. With that addition, we will get an admin claim on the Bob user when they're logged in.

I've got a new little block on the `AddMcpServer` HTTP transport options. The `ConfigureSessionOptions` property is a method where we can update things like available tools based on the logged-in user. I've got a simple function defined here that'll let us see that logged-in user in the debugger and confirm the claims.

Let's run it. Dashboard → Inspector → Connect, and I'll log in as **Bob**. Grant consent, and we hit that breakpoint. Let's look at this user object — check out the claims, expand the results, and there at the bottom, you can see the **admin role claim**. So we're connected, and let's list the tools. We hit our breakpoint again — I'll remove it for a sec. All four tools are visible for this Bob admin user, which is exactly what we want.

Let's disconnect and log in as **Alice**. I'll open the auth page and use the **Clear OAuth State** button. Now I'll connect again. I'm still logged in on the identity server as Bob on this consent page, so I'll click **Logout**, then **Yes**, close the tab, and reopen the inspector. Click **Connect**, and this time, I'll log in as Alice. Grant consent. Before I click List Tools, let's add that breakpoint back to see what claims we have on Alice. Now let's list the tools. Check out the user, open the claims, expand the results — there's no role claim here at all, admin or otherwise. So Alice is definitely not an admin.

When I hit Continue, I get an error because the UI timed out waiting for us while we were in the debugger. I'll list the tools again, hit Continue, and all four tools are listed. The API would prevent Alice from making successful calls, but it'd be better to limit the available tools right here. Let's do that now.

I've got a comment here about using **built-in authorization filters**. The docs tell us that we can use `[Authorize]` and `[AllowAnonymous]` attributes on our tools, prompts, and resources by simply adding them, and then this line to add authorization filters when we add the MCP server. Let's do this:

1. I'll add the `AddAuthorizationFilters` line. Since we're taking this approach, we might as well just add all the tools from this assembly — if we add more, they'll show up if the user is properly authorized. Then I'll comment out the two explicit lines.
2. Now, let's go to the admin tools and add an `[Authorize]` attribute with a `Roles` value of `admin`. This is just like we would do in any API or controller method. That requires a using statement.

Let's run it again. Inspector → Connect, and this is consent for Alice. List all the tools — and now, she only has the two tools which don't require admin. Let's disconnect. Connect again, log out as Alice, close the tab, relaunch the browser, connect, and log in as **Bob**. Grant consent and then list the tools — and now, all four are here. Now that we've got all of this working, let's move on to getting automated tests in place.

### Demo: Implement an Automated Test with an Access Token

In the spirit of not wasting your time by having you watch me type code, I've got some updated tests here — only slight updates to the tests we created in the last module:

- The first test I've converted into a **theory**, which lets me run the test multiple times with different parameters. I'm running it for both Bob and Alice. Instead of relying on an `McpClient` being present on the injected fixture, I'm calling a new method on the fixture to give me an `McpClient` based on the username and password combo from the args. Then I call `ListToolsAsync` and make sure that `get_products` is present.
- The next test is even more unchanged. The only difference is that it's getting a client based on the Alice user. Then it calls the `get_products` endpoint and makes some assertions on the response and content.

Let's look at the new `GetMcpClient` method. The `AppFixture` class doesn't have an `McpClient` property on it anymore, just that method to get a client. `InitializeAsync` only starts the Aspire solution in a test mode. Then, `GetMcpClient` takes user and password args and passes the fixture and those args to a `GetUserAccessTokenAsync` method that will get a bearer token. Then it sets up the transport options, but it initializes the additional headers dictionary and adds an authorization header with the access token. The creation of the `McpClient` uses the same call as before.

The `GetUserAccessTokenAsync` method is new. It gets the identity server HTTPS endpoint from the Aspire app, then sets up an `HttpClient` against it. It makes a **resource owner password request** on the identity server — it hits the token endpoint with a client ID of `testing.confidential` and provides a client secret, then passes the username and password. `testing.confidential` is a new client that I added to our identity server.

> The resource owner password flow is one that is **not generally recommended**, and I wouldn't use it outside of testing situations like this. For real applications where users or servers are authenticating, you should use a different approach.

In the `Config.cs` file of the identity server, in the Client section where the interactive clients are defined, I've got this new client called `testing.confidential`. It allows a resource owner password grant.

Let's try these tests. I'll put a breakpoint here and debug the `GetTools` test. Here's our breakpoint for getting an access token. Viewing the user parameter, this will be for the Alice user. Let's step into this method. Look at the `idSrvRoot` variable — it's `localhost` on a very different port than `5001`, which is fine based on the changes we've made to support a dynamic identity server in the Aspire app host. Since I'm taking a bit longer in the debugger for explanations, I start fighting timeout and task cancellations. I'll debug this test again and step over this get-access-token call. Looking at the variable now, it's an actual access token.

I still took too long here, so let's run these tests without the debugger. They both pass. Let's use the Test Explorer and run all the tests. You can see the different tests for Alice and Bob, and they all pass.

> **Homework:** These tests were pretty basic and didn't really test our role-based access. Your homework for this module is to verify the delete tool call with tests: check that Bob can successfully delete a product and Alice cannot, and maybe even update the get methods to allow anonymous access and test that. Check your results against the exercise files or the GitHub repo.

## Using an MCP Server from AI Agents

### Introduction and Our Tasks

Now that we've got our MCP server well-established with security and logging applied, it's time for us to get an application to use it, and that will get us more familiar with the client SDK and a couple of other features. Here's an overview of what we'll be up to:

- Create an **AI agent API method**, used by our Carved Rock web application, that will use our MCP server. We'll start with essentially Hello World, add some better configuration, then get our MCP server and its tools added into the mix. The agent will provide recommendations for people on the Carved Rock web app.
- Get the API itself working along with the MCP tool references, then see this called from our web app.
- Add functionality into the same API method for helping admin users invoke those two secure MCP tools that will delete a product or update the price of one. This will include adding a **prompt** to our MCP server. It'll also require **token forwarding** from the UI to the AI API and then to the MCP server.

### Demo: Add an AI Agent API Method

We're going to leverage the relatively new **agent framework** from Microsoft to create our simple AI agent. The whole purpose is to have something that can use our MCP server, and this implementation is less important. You could use any AI service, agent framework, or even just a locally hosted LLM that supports tool calling via MCP. We're going to use this agent framework, which merges concepts from both Semantic Kernel and AutoGen, and we'll use it with an **Azure OpenAI responses agent**.

The user guide lists three NuGet packages, but we'll only need two of them: `Azure.AI.OpenAI` and `Microsoft.Agents.AI.OpenAI`. The code we'll need to create:

1. Get an Azure OpenAI client.
2. Get a responses client.
3. Create an AI agent, provide some instructions, and ask it to do something.

For the AI service, I've got one set up in Azure. If you go into the Azure OpenAI service, you can get into the **Azure AI Foundry** portal. My endpoint is `kyt-openai.openai.azure.com`. There's an API key there. And if we look at the endpoints and go to the model deployments, there's a `kyt-gpt-4o-mini` deployment. We'll need the **URL**, the **API key**, and the **deployment name** in our API.

I've already made some updates. It's a brand new **agent controller** in the API project. This is just like any other API method: it's got an `[ApiController]` attribute, inherits from `ControllerBase`, and has a method called `Get` that allows anonymous access. It supports streamed output, so it's returning an `IAsyncEnumerable<string>`, and the cancellation token input param has an `[EnumeratorCancellation]` attribute on it. Right now this method doesn't take any other parameters. The steps in the code:

1. Read the API key from configuration. I've got it in my user secrets file — you'll need to provide your own instance and key somehow.
2. Create an Azure OpenAI client by referencing that URL for my `kyt-openai` instance, passing the key as an API key credential.
3. Get a responses client by referencing my deployment name. (The two pragmas disabling warnings are because this particular code is still prerelease. We'll be cleaning this up in a minute.)
4. Create the Hello World version of an agent by providing an instruction that they're good at telling jokes and giving a name to this agent.
5. An `await foreach` loop to `RunStreamingAsync` the question of having it tell us a joke about Alice in Wonderland. The response comes back in parts since we're streaming, and we `yield return` each part.

If we look at the `Directory.Packages.props` file, you can see the new package references I've added: `Aspire.Azure.AI.OpenAI` (you'll see why in the next clip), `Azure.AI.OpenAI` (prerelease), and `Microsoft.Agents.AI.OpenAI` (also prerelease).

Let's make sure it works. From the dashboard, I'll launch the Swagger link for the API. Here's this new agent controller with a GET route that should allow anonymous access. I'll expand it, try it out, and hit **Execute**. It worked — the response is a bunch of chunks since we're streaming it, but it is a joke about Alice bringing a ladder because she wanted to bring new heights to her adventures.

Let's go look at the traces — the last one for the GET Agent call. We can see the GET request, and then a POST call to OpenAI. Helpful, but there's a lot more power that Aspire brings to the table for these types of calls with AI. So let's get that set up and eliminate some of our hard coding in the next clip.

### Demo: Improve Configuration and Telemetry with Aspire

This clip won't change the behavior of our simple starter agent at all, but it will eliminate some hard coding, get rid of these pragma warning disable lines, and significantly improve the experience we have in the traces. I've got a link to some docs about the **Aspire Azure OpenAI integration**. The hosting integration section deals with provisioning Azure resources — we're going to skip that since we already have a service. We're going to use the **client integration**, which adds OpenTelemetry and simplifies our setup.

In `Program.cs` of our API, we'll add a line that will add an Azure OpenAI client. Then we'll specify the connection details rather than look for a connection name that would have been set up in the app host. Then we could inject an `OpenAIClient`, but an even better approach is to register an injectable `IChatClient` with the `AddChatClient` extension method. Then our API agent method will be able to inject an `IChatClient`.

The **Observability and telemetry** section has more good info. One of the big reasons to use this Aspire client integration package is because it adds OpenTelemetry traces and metrics for us:

- The tracing section says that it'll emit tracing activities from `Experimental.Microsoft.Extensions.AI`.
- Telemetry is only recorded (for now at least) when you're using this `IChatClient` interface.
- Telemetry includes **token counts but not message content**. For development, it'll be good for us to see those messages. You can use configuration to control it for higher environments.

Let's get our code updated:

- I'll paste in a line to add an Azure OpenAI client, which has a string parameter that we won't really use (we didn't set up this named instance in our app host). We use the `configureSettings` parameter with a delegate to specify connection details — the endpoint (the URL for my Azure OpenAI service, read from configuration as the `AI:Connection:Endpoint` value, converted to a `URI`), and the key property (the API key I have in user secrets).
- In order to see the messages in our new trace content, I'll explicitly set the `EnableSensitiveDataLogging` property to `true`.
- Add a chat client with the extension method and give it the name of our model deployment.

Now let's update the API method to use the injectable `IChatClient`. I'll update the primary constructor to inject an `IChatClient`, and then I can get rid of all this code before we create an AI agent. I can just create the agent by referencing this `IChatClient` instance.

Before we run this, let's quick look at the extensions file in our service defaults project. In the `ConfigureOpenTelemetry` method, where before we added a meter and a source for MCP itself, I've added another of each for `Experimental.Microsoft.Extensions.AI`.

Let's run this. I'll go back to the Swagger doc, expand our agent GET method, try it out, and execute it. It works, and our agent code itself is much simpler. Let's look at the trace. That same call now has four items in the trace, and one of them has this **AI sparkle**. Let's click on it. The LLM took 1.05 seconds and consumed 47 tokens. You can see the system instructions, the user input, and what the assistant responded with. This is only going to get more useful when we see how MCP calls are included. Let's get our MCP server added to the mix next.

### Demo: Add Our MCP Tools to the Agent

Now that we've got an AI agent basically in place with great telemetry, it's time to get our MCP server added to the mix and update the prompt for our use case. In the `.csproj` file, I've added a package reference to `ModelContextProtocol`. We're going to need to get an `McpClient` here so that we can provide the tools to our AI agent, and we've seen code that does that in our tests.

I'll paste in some basic code to get this to work at first, then refine it:

1. I'm setting up the client transport variable with a **hard coded endpoint** (something we'll do differently soon), but this URL should work to start with. Then we get an `McpClient` the same way we saw in the tests.
2. Now let's get a list of the tools with the `ListToolsAsync` method.
3. Make the tools available to our agent by setting the `tools` property to this new list of tools, wrapped in a collection expression.
4. Give the agent a better name, and paste in some new simple instructions for a prompt. It's saying we want the agent to be an assistant that can make recommendations about our products, sets a limit on how many it can recommend, and tells it to be polite if it can't do something.
5. Ask a more relevant question in the user request — that we've got a hike coming up and we're asking for some recommendations. So the agent would need to use the `get_products` tool to get the products and then review them to see which ones it'll recommend.

Okay, here's the dashboard. Back in the API Swagger UI, I'll expand the agent GET method, try it out, and hit **Execute**. We get a response, and it's actually looking pretty good — three product recommendations, though it's a little hard to read here since it's streamed.

Let's look at the trace. There's a lot going on: you can see the `initialize` call to the MCP server and the `tools/list` call. And right under `orchestrate_tools`, there's the first call to Azure OpenAI. Let's click the sparkle — there's our new system prompt and the hard coded question. The next step is for it to call the `get_products` method. There's this **Next** feature at the top of this dialog that lets us go to the next AI entry. It's the same initial content, but now the response of the `get_products` call is there, and the output is a much more readable version of that streamed content with the recommendations. Let's add a parameter for the message and try calling it from our UI in the next clip.

### Demo: Implement Token Forwarding, and Include the UI

Now it's time for some real magic. We'll get this agent AI API method to be called from our UI. I've done some refactoring to our API method to get the `McpClient` set up a little more cleanly. I've defined a helper method to get an `McpClient` that takes an `IConfiguration` parameter and an `IHttpContextAccessor`:

- If we don't have an authenticated user in the API method based on the `HttpContext`, we'll get an **anonymous** `McpClient`. The anonymous client is what we've seen before, but for the URI, I'm no longer hard coding it — I'm getting it from a method I'll show you.
- If we have an authenticated user, we'll get a **token forwarding** client. It uses the same new method to get the MCP endpoint, and it adds an authorization header by getting an access token from the `HttpContext`.

The way I'm getting the MCP server URL is a configuration-based hierarchy:

1. Aspire will set this `Services:mcp:http` endpoint (it's a list, and I'm grabbing the first item).
2. For higher environments, I can use this `McpServer` config setting that I don't have set anywhere right now.
3. If I don't have either of those, we can fall back to a hard coded value. (This value won't work, but it won't be used because we'll have the value from Aspire.)

The new method to get the access token from the `HttpContext` finds the header from the incoming request and just returns the value of that auth header — it's just forwarding what was already there.

Let's try this out and make sure it works for both anonymous and logged-in users. Here's the dashboard and the Swagger link. I'll expand the agent, try it, and execute it — it still works. Let's go over to the web app and try things from there. I'll sign in. In another browser, I was signed in as Bob already, so we didn't see a sign-in screen. Notice we've got an **Admin** link available. I'll go to the footwear page where I had AI help me add a little chat widget wired up behind this **Send** button. Nothing happens if I don't put any text in the box, but our API still doesn't accept input parameters. I'll just type `a` here and hit **Send**. It's thinking, and then we get a nicely streamed and formatted response.

Let's look at the trace. This GET Listing at the bottom is the one we're interested in, and there's plenty of activity. One of the main things I was interested in is whether the MCP server knows who was connecting to it this time. Let's click this log entry — the dot for this `tools/list` activity. There's the Bob Smith email address and the user ID. It means the **token forwarding is working**, and we still have those same sparkle items for the actual LLM calls.

Let's look at the new code in this UI. The page is the listing page — let's look at the code-behind file, which will run on the server. There's this new `OnGetChat` method that takes a message that would have come from the user. It creates an `HttpClient` for the client named `AI` and calls the `GetAgent` method with the message. The named client was registered in `Program.cs`, and that needs its own token forwarding logic. Since this is the application where the user is actually logged in, a better way to manage access tokens is a good idea since they might need to be refreshed. In `Program.cs`, where it sets up its OpenID Connect option, there's an option to **save tokens** that's important. And a few lines down, there's this slick little line to add **OpenID Connect access token management** and then to add a user access token `HttpClient` called `AI` that will point to our API and handle any token refreshes and forwarding. These features are made available via the NuGet package `Duende.AccessTokenManagement.OpenIdConnect`.

The rest of the logic in the listing code file handles the streaming response using **server-sent events**. The `Listing.cshtml` file has a bunch of JavaScript that will invoke that code-behind and listen for the streamed response content. Look at that at your leisure — it's not the point of the course. In the next clip, we'll make the message text actually work in the API so we can send different requests, and we'll see if we can get admin features working in this chat for our Bob admin user.

### Demo: Add "Slash" Command with Authorized Tools

We're going to be adding some admin functionality to our agent in this clip. First, let's add a `message` parameter here in the API so that we can actually type something in the UI that'll be passed into this AI agent. And then when we invoke the agent, let's replace our hard coded request with the input message.

To set up our admin functionality, I'd like to have a completely different prompt for when an admin uses a **slash command** for administrator features. Let's create a prompt in the MCP server and expose it from there. I'll add a new file to the MCP server called `AdminPrompt.cs`. I've got an `[Authorize]` attribute on the prompt so that only admin users will be able to get to it, and the name is `admin_prompt`. Then the prompt itself doesn't take any input parameters but returns the text for the admin prompt. It says it's there to help administrators and to confirm that actions have completed when they're done.

Then in `Program.cs` for the MCP server, when we add the MCP server, we now need to also add **prompts** from the assembly.

Now let's make our AI agent code a little smarter by adding a method to get a prompt. Basically, if the input message starts with `/admin` and the user is an admin, then we'll get the admin prompt from the MCP server. We'll read the result, and I've got to get the full prompt put together from the set of messages we get back. If the conditional is false, we'll just use the original prompt. Now I can set a `prompt` variable to the result of calling this new function, and update the `CreateAIAgent` to use the prompt we just got.

Let's see if the API supports a deletion from the agent method now. In the Swagger UI, I'll **Authorize** (the client is correct, and I'll select all the scopes, then click **Authorize** — I was already signed in as Bob so I don't get a challenge). I'll expand the Agent GET method, try it out, type `/admin delete product 2` and hit **Execute** — and we hit this exception. We're in this new method to get the prompt, and we're failing when we call the MCP server to get the admin prompt. So the API code did recognize the `/admin` part and that the user is in the admin role. But when we called the MCP server, it's saying that `prompts/get` is not available.

It turns out that fixing this is pretty easy — a quick oversight on my part. I missed the `McpServerPromptType` attribute on our new Prompt class. Since we're loading prompts from the assembly, I needed to include this attribute.

Let's try this again. Authorize with all the scopes, expand the Agent GET method, try it, and type `/admin delete product 2`. And it works — it says it successfully deleted product 2. Let's check by calling the get-all-products API method — there's one and then three, but no two.

Let's see how this works from the UI. I'll go into the footwear page, and I haven't signed in, so I'm definitely not an admin here. I'll type `/admin delete product 1`, and I get a polite message from the API where it says it can't do that. I'll try getting a recommendation that includes some kayaking, and that works great. I'll let you try signing in as Bob and trying a delete product or updating its price, but it should work, even if you try it by name.

## Creating Developer-targeted MCP Servers and Deploying them to NuGet.org

### Introduction and Approach

We're on the home stretch now and have completed the work on our Streamable HTTP server, which is what you'd most likely use to expose business functionality from your applications to AI. But now, we're going to look at the **standard IO transport** a little more and create an MCP server that could be used by developers and made available to coding assistants like GitHub Copilot, Cursor, and Claude Code, among others.

We're still using the fictional company of Carved Rock for our use case. For this example, we'll create an MCP server that Carved Rock developers could use. Here's the plan:

- Use a template from `Microsoft.Extensions.AI.Templates` to get ourselves started, doing this from **VS Code**.
- Try it out in VS Code and see what the develop and test process looks like. That'll have us create a new file that tells VS Code and GitHub Copilot about a new MCP server we want it to be able to use.
- Take the template further and add some functionality to generate some **test data** for Carved Rock products.
- Find out what kind of **metadata** we should provide when publishing an MCP server and where that metadata belongs — one spot for the MCP server itself, and since we're publishing to NuGet, some metadata in the project file.
- Get our new MCP server packed up and **publish it to NuGet**.
- See how to **use** this published MCP server.

### Demo: Create an MCP Server Using a Template

There's really good documentation for a quick start on publishing an MCP server to NuGet on Microsoft Learn. These docs are great and will largely follow the process here. We need to install some templates and then use the MCP server one. Once we create it, we'll use a special file to tell VS Code and GitHub Copilot that there's a new MCP server it can use.

Okay, here I am in a terminal. Let's install those templates:

```bash
dotnet new install Microsoft.Extensions.AI.Templates
```

That has given us two new templates: an **AI chat web app** and an **MCP server console app**. Let's create a new project using the MCP server template:

```bash
dotnet new mcpserver -n CarvedRock.Developer
```

Let's go into the new directory and open VS Code. It's a nice small little project: there's a `README`, `Program.cs` for the entry point and wire-up, the project file, a directory for `tools`, and a `.mcp` directory where the `server.json` file for metadata lives.

Let's look at `Program.cs`, nice and small:

- The first line is `Host.CreateApplicationBuilder` rather than `WebApplication.CreateBuilder` like we do for ASP.NET MCP servers.
- It's redirecting all log messages to **standard error** since MCP makes its calls using standard output (the JSON-RPC calls we looked at in the inspector).
- We add an MCP server, use the standard IO transport, and we've got one class of tools we're making available.
- Then we build the app builder and run it. This'll be a **long-running process**, one that exits when we close the IDE or explicitly stop it.

The `.csproj` file has a couple of interesting things:

- Lots of **runtime identifiers**, which is required for creating self-contained versions that can run on a variety of platforms. We need an explicit build for each one.
- We're packing this project as a tool and using `McpServer` as the type.
- This is published as **self-contained**, which goes hand in hand with that set of runtime identifiers.
- We're including the `server.json` and `README` files in what we pack so that content will be available on nuget.org.
- The package references: hosting extensions and MCP.

Let's check out the tool code — none of this should be new to you. It's a simple tool to get a random number between the two input parameters. It's got a tool attribute, a description, and we don't have the `McpServerToolType` attribute on the class since we saw the `WithTools` call using the generic type in `Program.cs`.

The `.mcp` folder has a single file called `server.json`. This has some metadata (that we'll update in a bit) that describes our MCP server. That's the whole project. Let's see how we can test this locally without publishing it next.

### Demo: Test the MCP Server from VS Code

Let's try out our MCP server interactively here in VS Code. In order to tell VS Code about our server, we need to have a `.vscode` folder. I'll create one, then add a file called `mcp.json`. I'll paste in some content (found in that README or the quick-start doc):

- Change the name of this MCP server to `CarvedRock.Developer` to match our project (just a name that can be whatever, but it helps us keep things straight).
- Update the path to project file. Since our project is in the root directory, I can just give the `.csproj` file name.
- Add a closing curly brace.

This is a file that contains a list of custom servers for this workspace. I've only got one, and it's a standard IO server. It'll run the `dotnet` command with args of `run --project` of my new project, so this command from the root of my workspace would be:

```bash
dotnet run --project CarvedRock.Developer.csproj
```

When I added that closing curly brace to correct the syntax, I've now got the option to **start** this MCP server. Let's do it. Now that it's running, we can both stop and restart it. You can also see that it's exposing one tool.

Let's open our AI chat window. In the chat box, we need to be in **agent mode**. There's a tools icon, and mine is showing a warning — we need to click that. We can see a list of tools that this agent has available, and the warning is that I may experience trouble if I have more than **128 tools**. I'll collapse this built-in node and uncheck most of the tools I don't need, and my warning has gone away. Down at the bottom, you can see our new `CarvedRock.Developer` MCP server. I'll expand it, and sure enough, you can see that `get_random_number` tool. I'll make sure that's checked, then click **OK**.

Now, let's try it. I'll go to the chat and ask it to give me a random number between 1 and 75. It's thinking, and then it shows me that it used the `get_random_number` tool from our `CarvedRock.Developer` MCP server. It gave a result of 50, which is fine. It's also showing that this was auto-approved for my profile. You're probably being asked to allow the tool to run, and you can do that always, for this time, or for this session. During my testing, I approved it for the workspace. We've got our server running locally, and it's accessible via our agent chat. Let's add some custom functionality next.

### Demo: Add Custom Functionality for Test Data Generation

We're going to add some custom test data generation capabilities. I'll create a new file in the `tools` folder called `ProductGeneratorTool.cs`, then paste in some content. The code is relying on a nice little NuGet package called **Bogus**. I'll open a terminal and add it:

```bash
dotnet add package Bogus
```

Now I can fix these issues by adding some using statements. (The `ClampLength` method requires `using Bogus.Extensions`.) Let's look at this new tool:

- It's got the tool attribute and a good description that says it'll generate Carved Rock test products.
- It takes a single number as an optional input parameter and would use `10` as a default if no value is provided.
- It generates that number of products with a `ProductFaker` object that has some interesting rules, all using the Bogus library:
  - Using its `Commerce` class to generate a product name and description, setting limits on how long each can be with the `ClampLength` extension.
  - For the category, picking a random one of four items.
  - The price is a random value in a range dependent on the category.
  - An image URL is used from its `PicsumUrl` feature.

You could also define an AI prompt to have some of these constraints. But to keep things simple, I just wanted an example of how you might encapsulate some interesting logic for test data generation into an MCP server so that every developer didn't need to know about these constraints, but could easily generate their own data.

Let's add this new tool: `WithTools` and reference our new `ProductGeneratorTool` class. Let's build this — **Ctrl+Shift+B**, and I'll choose this `dotnet: build` option. We're getting an error that the process cannot access the file. Remember that I said our MCP server was a **long-running process** that just keeps running? Our server is still running — we never did anything to stop it. If we go back to our `.vscode/mcp.json` file, we can use the **stop** option to stop the process. Now if I build it again, it works with no errors.

Let's use this start action to start our server back up, and open that chat window again. Let's use the chat to see if we can get some test products created. I'll collapse the built-in list, expand this `CarvedRock.Developer` server, and there's our new tool. Let's try out this prompt. Thinking — looks like it's found our new tool, and then we get five products. But if we really wanted to use them, we'd need this in JSON format. Let's see if that can work. And there we go — now we've got some test data that we could use in our testing.

### Demo: Metadata for MCP Servers

Let's have a quick look at the metadata that's used when we publish an MCP server as a NuGet package.

The first place we're looking is the **`.csproj` file**. We want to support different runtimes for this as self-contained executables that don't require specific installs of SDKs — that's why this list of runtimes is here, which also drives the settings about self-contained and single files. We also have the `PackAsTool` flag set, and the package type is `McpServer`. The NuGet package information a little further down is important, and I've updated some of it:

- Set the package ID to the name that we want published.
- The version I left alone, but you'd update this as you have new versions, and it has to agree with what we'll see in the `server.json` file.
- Some tags you can specify — add or tweak these if you want.
- The description is useful to update.
- I recommend including the **project URL** and **repo URL** properties.
- We're including the `README` and the `.mcp\server.json` file so that they can be used in the publish process.

Now let's look at the **`.mcp\server.json` file**:

- The description here is good to update — I've used the same text as in the project file.
- The **name** needs to be unique, and I've followed the suggested convention.
- The version should match what we had in the `.csproj` file, and it's specified in two different places here, so be careful of that.
- The **identifier** should match the package ID in the `.csproj` file.
- The repository URL is here too, and I've updated this value.

You might be wondering about the **package arguments and environment variables**. We haven't created any tools or functionality that requires these, but there's an example in the quick start. One thing that's common is that you might need some kind of API key or personal access token, and you could reference an environment variable for these values. That example takes a comma-separated value as an environment variable for some options, then shows how to update the `server.json` metadata so that a user knows to provide these values.

As with any package you're publishing, you should have a good **README**. I've updated the starter README with content that gives a description of what it's doing and a summary of the tools it provides. The original README content of the template is still applicable since this is an example, but in a real-life MCP server, you'd probably remove much of this content.

### Demo: Pack and Publish to NuGet.org

The steps to get an MCP server published to NuGet are in the quick start. We'll run the `dotnet pack` command with the release configuration, then publish it with the `dotnet nuget push` command. The push command in the docs is a **Bash** one, and since I'm on Windows, we'll need to tweak the way we reference the `.nupkg` files. This push command requires an **API key** for NuGet.

On nuget.org, one of the first things you'll need to do is sign in (and you may need to create a free account). On the **Manage API keys** page (click your username → **API Keys**), I've created a key called `mcptester` that'll let me push some packages. I've got the key value copied.

Let's do all of this. I'm back in VS Code with a terminal in the root directory of the workspace:

```bash
dotnet pack -c Release
```

That's busy creating self-contained tools for all of the different runtimes the template has targeted, and they're all done. I'll clear the screen (`cls`). Now we're going to run the `dotnet nuget push` command. Note the forward slashes in the docs version — this didn't seem to work for me and was giving me "no file found" errors. I'll paste in the updated command I used in this **PowerShell** terminal. It's got the path to my `.nupkg` files, but using backslashes, plus my API key and the source of this NuGet URL:

```powershell
dotnet nuget push .\bin\Release\*.nupkg --api-key <YOUR_API_KEY> --source https://api.nuget.org/v3/index.json
```

Let's run this. It seems to be pushing the packages, and it finishes with no errors. There are some warnings about including a license, and you can do that on your own if you're publishing something for real or for the general public that's beyond just a sample.

If we go back to nuget.org and choose **Manage Packages**, the place we'll find our MCP server now is in the **unlisted packages**. This isn't because there was any trouble or a beta version, but rather because it hasn't been **indexed** by NuGet yet. You'll need to wait about 10 or 15 minutes before you can actually use any package you publish here. You can refresh the package page while you wait — it'll lose this warning and have more content when it's published for real. We'll see what that looks like and use this new MCP server from Visual Studio in the next clip.

### Demo: Use a Published MCP Server

We just published our MCP server to NuGet, and this is what the page should look like once it's published fully and indexed. NuGet knows it's an MCP server, so there's this block at the top that's what we should be able to put into an `mcp.json` file for our IDEs to know about this MCP server. The page has links to my GitHub repo under the project website and the source repository (since I included those values in the `.csproj` file when we packed it up). The README shows the custom content I added about the tools.

If you want to try using this server without actually publishing your own package, you should be able to do it by following these steps. Let's copy this server content (I've also pushed this into a GitHub repo — the same stuff we've been looking at). Now let's go over to **Visual Studio**, which has no previous knowledge about this MCP server at all.

For Visual Studio, I can add a file to the solution called `.mcp.json` that we can use to make this MCP server available as Copilot tools when I have this solution open. I'll paste in the content. This is a little different than the `dotnet run` command that we saw for our local project — this is using **DNX**, which requires **.NET 10** to be installed. But beyond that, you wouldn't need any other specific runtime, even for .NET 9 MCP servers. The args are the package we're using, and notice that this does refer to a **specific version**.

> As a little aside, I've put together a quick reference on how you can add MCP servers for both local execution in a Visual Studio solution or a VS Code workspace, or if you want to make the MCP server available **globally** whenever you have those IDEs open. The file contents are the same, but the names and locations are a little different for each IDE.

When I saved this file, there's some extra content that would let me restart this MCP server — that must mean it's running. Just like in VS Code, I need to be in **agent mode**, and we can click this icon to see the available tools. Right when I click it, we can see our new `CarvedRock.Developer` MCP server with its two tools. Let's make sure they're enabled.

Now, let's see if we can get it to generate 20 products. It's recognized our tool and will be providing an argument of 20. It's asking us for consent to keep moving — we made the tool so we can confirm this. I could just click this button to confirm once, or choose the drop-down and always allow it or just for this session. I'll choose **Always allow**. It not so helpfully just tells us that it's generated 20 products, but it does ask if we want a specific format. I'll say JSON, and then it outputs some JSON that doesn't look as nice here, but this is still pretty cool.

This solution gets its data from a `SeedData.json` file over in the data project, and we could replace this content or have developers provide their own. I've got two final notes:

- Since this new file in the solution, `.mcp.json`, is in the root folder, we could add it to a `.gitignore` file or not. If we don't add it and this file gets committed in the repo, every developer that clones it is going to have this `CarvedRock.Developer` MCP server at least available in their MCP tools. They'd still need to enable it and grant permission, but they wouldn't have to find it and add it. If you wanted to exclude this file from the repo, developers would need to find the server on their own.
- If you're on nuget.org, one of the things you can do now is **search for MCP servers**. If we do that and apply the sort by recently updated, you can see my `CarvedRock.Developer` one.

Let's wrap up this journey we've been on with some final thoughts and recommendations.

### Closing Remarks

In closing, I wanted to come back to a slide we saw earlier in the course. I'm not going to go through it point by point this time, but I do think it's worth coming back to.

One of your biggest challenges may not be the code of your MCP server, but rather **figuring out what to expose as a tool, a prompt, or a resource**. Just wrapping an API may not be the best approach. You really need to think about what kinds of things you want your AI services to do, whether they're on behalf of people or some kind of background process. And then think of all the different ways we authenticated and authorized against our MCP server, and make sure that you've got a good way to **trace and understand activity** that's happening in your server.

If you've stayed with me this far, I'm confident that you'll have what it takes to create your own MCP servers that'll unlock awesome AI capabilities in your applications. **Happy building.**
