<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="WebApplication._Default" %>

<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="telerik" %>

<asp:Content runat="server" ID="FeaturedContent" ContentPlaceHolderID="FeaturedContent">
    <section class="featured">
        <div class="content-wrapper">
            <hgroup class="title">
                <h1><%: Title %>.</h1>
                <h2>WebAPI and custom JSON Serialization</h2>
            </hgroup>
            <p>
            </p>
        </div>
    </section>
</asp:Content>
<asp:Content runat="server" ID="BodyContent" ContentPlaceHolderID="MainContent">
    <telerik:RadScriptManager ID="rScriptManager" runat="server"></telerik:RadScriptManager>
        <telerik:RadAjaxManager ID="RadAjaxManager1" runat="server" UpdateInitiatorPanelsOnly="true">
        <AjaxSettings>
            <telerik:AjaxSetting AjaxControlID="RadGrid1">
                <UpdatedControls>
                    <telerik:AjaxUpdatedControl ControlID="RadGrid1" LoadingPanelID="RadAjaxLoadingPanel1" />
                </UpdatedControls>
            </telerik:AjaxSetting>
        </AjaxSettings>
    </telerik:RadAjaxManager>
    <telerik:RadAjaxLoadingPanel runat="server" ID="RadAjaxLoadingPanel1">
    </telerik:RadAjaxLoadingPanel>
    <h3>We suggest the following: Bind RadGrid to a WebAPI service generated with Telerik Data Access</h3>
    <ol class="round">
        <li class="one">
            <h5>The result:</h5>
            <br />
            <telerik:RadGrid ID="rGrid" EnableViewState="False" runat="server" 
                AllowPaging="True" AutoGenerateColumns="False" PageSize="10">
                <ItemStyle Wrap="false" />
                <MasterTableView TableLayout="Fixed">
                    <Columns>
                        <telerik:GridNumericColumn DataField="ProductID" HeaderText="ProductID" HeaderStyle-Width="100px"
                            FilterControlWidth="50px">
                        </telerik:GridNumericColumn>
                        <telerik:GridBoundColumn DataField="ProductName" HeaderText="Category Name">
                        </telerik:GridBoundColumn>
                        <telerik:GridBoundColumn DataField="CategoryName" HeaderText="Category Name">
                        </telerik:GridBoundColumn>
                        <telerik:GridBoundColumn DataField="QuantityPerUnit" HeaderText="QuantityPerUnit">
                        </telerik:GridBoundColumn>
                        <telerik:GridBoundColumn DataField="UnitPrice" HeaderText="UnitPrice">
                        </telerik:GridBoundColumn>
                    </Columns>
                </MasterTableView>
                <PagerStyle AlwaysVisible="true" Mode="NextPrevAndNumeric" PageSizeControlType="RadComboBox"></PagerStyle>
                <ClientSettings>
                    <DataBinding Location="http://localhost:50352/api" ResponseType="JSON" CountPropertyName="Count" DataPropertyName="Items">
                        <DataService TableName="CustomProducts" Type="OData"/>
                    </DataBinding>
                </ClientSettings>
            </telerik:RadGrid>
        </li>
        <li class="two">
            <h3>How to achieve it:</h3>
            <ol class="round">
                <li class="one">
                    <h5>Create a model based on the Northwind database. Make sure enable the default implementation of the ISerializable interface.</h5>
                    <br />
                </li>
                <li class="two">
                    <h5>Create the ProductCustomized class and use the Product class as its base class.</h5>
                    <br />
                </li>
                <li class="three">
                    <h5>In the ProductCustomized class, override the implementation of the GetObjectData() method.</h5>
                    <br />
                </li>
                <li class="four">
                    <h5>In the ProductCustomized class, introduce a property that will hold the name of the product's category.</h5>
                    <br />
                </li>
                <li class="five">
                    <h5>In a partial class, extend the Product class, and allow the Category navigation property to be serialized.</h5>
                    <br />
                </li>
                <li class="six">
                    <h5>Use Services Wizard to generate a WebAPI service for the Product persistent class.</h5>
                    <br />
                </li>
                <li class="seven">
                    <h5>Extend the generated service with the ProductCustomizedRepository class, and the CustomProducts controller.</h5>
                    <br />
                </li>
            </ol>
        </li>
    </ol>
</asp:Content>
