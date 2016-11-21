<%@ Control Language="C#" AutoEventWireup="true" CodeFile="FeedProvider.ascx.cs" Inherits="RockWeb.Plugins.com_shepherdchurch.Roku.FeedProvider" %>

<div class="alert alert-danger">
    <div>If you were not an Administrator you would have seen only the following XML content:</div>
    <div>
        <pre><asp:Literal ID="ltContent" runat="server" /></pre>
    </div>
</div>

<div class="alert alert-info">
    <asp:Literal ID="ltDebug" runat="server" />
</div>