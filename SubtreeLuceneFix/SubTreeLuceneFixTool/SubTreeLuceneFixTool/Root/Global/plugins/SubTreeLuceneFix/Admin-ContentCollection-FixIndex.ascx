<%@ Control Language="C#" AutoEventWireup="true" Inherits="Azrael.LuceneIndexTools.SubTreeLuceneIndexFixContentCollectionView" %>

<asp:Button runat="server" ID="FixIndex" Text="FixIt" OnClick="FixIndex_OnClick"/>
<asp:Button runat="server" ID="FixStatus" Text="Status" OnClick="FixStatus_OnClick"/>
<asp:Button runat="server" ID="FixCancel" Text="Cancel" OnClick="FixCancel_OnClick"/>
<asp:Button runat="server" ID="FixList" Text="List Items" OnClick="FixList_OnClick"/>
<asp:Button runat="server" ID="FixDiffList" Text="List DiffItems" OnClick="FixDiffList_OnClick"/>
<asp:Button runat="server" ID="ClearCache" Text="Clear Cache" OnClick="ClearCache_OnClick"/>

<div class="fixLuceneStatus"> 
    <span>Állapot: </span>
<asp:Label runat="server" ID="StatusLabel" Text="Nem fut jelenleg javito task"></asp:Label>
</div>
<div class="fixLuceneDbItemCount">
    <span>Db alapú elemszám: </span><span><% = DbNodeItemList.Count %></span>
</div>
<div class="fixLuceneLuceneItemcount">
    <span>Luc alapú elemszám: </span><span><% = LucNodeItemList.Count %></span>
</div>
<div class="flawedItemCount">
    <span>Különbségszám: </span><span><% = GetDiffCount() %></span>
</div>

<%--TESZT ESETEK START--%>
<%--<br/>
   
    <% this.Model.Content.ChildrenDefinition.AllChildren = true;
        this.Model.Content.ChildrenDefinition.EnableLifespanFilter = SenseNet.Search.FilterStatus.Disabled;
        this.Model.Content.ChildrenDefinition.EnableAutofilters = SenseNet.Search.FilterStatus.Disabled;
        this.Model.Content.ChildrenDefinition.ContentQuery = string.Empty;

        var query = "+InTree:'" + this.Model.Content.Path + "' -Path:'"+this.Model.Content.Path+"'";
        SenseNet.Search.QuerySettings qs = new SenseNet.Search.QuerySettings();
        qs.EnableLifespanFilter = SenseNet.Search.FilterStatus.Disabled;
        qs.EnableAutofilters = SenseNet.Search.FilterStatus.Disabled;
        var result = SenseNet.Search.ContentQuery.CreateQuery(query, qs).Execute();
    %>
    
    <div>
    <span>ContentQuery query: </span><span><% = query %></span>  
        </div><div>
    <span>ContentQuery Count elemszám: </span><span><% = result.Count %></span>
            </div><div>
    <span>ContentQuery Identifier elemszám: </span><span><% = result.Identifiers.Count() %></span>
                </div><div>
    <span>ContentQuery Nodes elemszám: </span><span><% = result.Nodes.Count() %></span>
                    </div>
    <br/>
    <div class="fixLuceneLuceneItemcount">
    <span>Content.All Count elemszám: </span><span><% = SenseNet.ContentRepository.Content.All.DisableAutofilters().DisableLifespan().Count(c => c.InTree(this.Model.Content.Path) && c.Path != this.Model.Content.Path) %></span>
</div>
<div class="fixLuceneLuceneItemcount">
    <span>Content.All ToArray elemszám: </span><span><% = SenseNet.ContentRepository.Content.All.DisableAutofilters().DisableLifespan().Where(c => c.InTree(this.Model.Content.Path) && c.Path != this.Model.Content.Path).ToArray().Length %></span>
</div>
<br/>
                <%
                      var q = LucQuery.Parse(query);
                      q.EnableAutofilters = FilterStatus.Disabled;
                      q.EnableLifespanFilter = FilterStatus.Disabled;
                      var lucArray =  (from lucObject in q.Execute() select lucObject).ToArray();
                     %>

  <div>
    <span>LucQuery query: </span><span><% = query %></span>  
        </div><div>
    <span>LucQuery Count elemszám: </span><span><% = lucArray.Count() %></span>
            </div><div>
    <span>LucQuery Path elemszám: </span><span><% = lucArray.Select(lo => lo.Path).Count() %></span>
       --%>         </div>

<%--TESZT ESETEK END--%>
<hr/>
<div class="fixLuceneItemList">
<asp:Repeater runat="server" ID="FixItems" >
    <HeaderTemplate>
        <table>
            <tr>
                <th>no.</th>
                <th>Version Id</th>
                <th>Node Id</th>
                <th>Path</th>
                <th>Is in DB</th>
                <th>Is in Lucene</th>
            </tr>
    </HeaderTemplate>
    <ItemTemplate>
        <tr>
            <td><%# Container.ItemIndex + 1 %></td>
            <td><%# Eval("VersionId") %></td>
            <td><%# Eval("NodeId") %></td>
            <td><%# Eval("NodePath") %></td>
            <td><%# Eval("IsInDb") %></td>
            <td><%# Eval("IsInLucene") %></td>
        </tr>
    </ItemTemplate>
    <FooterTemplate>
        </table>
    </FooterTemplate>
</asp:Repeater>
</div>
