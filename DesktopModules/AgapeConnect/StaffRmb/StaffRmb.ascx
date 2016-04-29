<%@ Control Language="VB" AutoEventWireup="false" CodeFile="StaffRmb.ascx.vb" Inherits="DotNetNuke.Modules.StaffRmbMod.ViewStaffRmb" %>
<%@ Register TagPrefix="dnn" TagName="Label" Src="~/controls/LabelControl.ascx" %>

<script src="/js/modernizr-custom.js" type="text/javascript"></script>
<script src="/js/jquery.watermarkinput.js" type="text/javascript"></script>

<script src="/js/tree.jquery.js"></script>
<link rel="stylesheet" href="/js/jqtree.css" />
<style>
    input[type="number"] { text-align:right; }
    .abutton.delete:hover  { background-image:none; color:white; background-color:rgba(255,0,0,0.5); }
    .abutton.go {background-image:none; background-color:#A1D490;}
</style>

<script type="text/javascript">
    "use strict";
    var previous_menu_item = null;
    function selectMenuItem(menu_item) {
        deselectPreviousMenuItem();
        menu_item.style.fontWeight = 'bold';
        menu_item.style.fontSize = '9pt';
        $(menu_item).parent().next().children().css('visibility', 'visible');
        previous_menu_item = menu_item;
    }

    function deselectPreviousMenuItem() {
        if (previous_menu_item === null) {
            return;
        }
        previous_menu_item.style.fontWeight = 'normal';
        previous_menu_item.style.fontSize = '10pt';
        $(previous_menu_item).parent().next().children().css('visibility', 'hidden');
    }
   
    function loadRmb(rmbno) {
        var is_chrome = navigator.userAgent.toLowerCase().indexOf('chrome') > -1;
        var url = trimBefore(trimBefore(document.location.href, "/rmbno"), "/rmbid");
        if (is_chrome) {
            openInBackgroundTab(url+"/rmbno/"+rmbno);
        } else {
            window.open(url+"/rmbno/"+rmbno, "_blank");
        }
    }

    function trimBefore(text, delimiter) {
        var pos = text.toLowerCase().indexOf(delimiter.toLowerCase());
        if (pos >=0 ) {
            return text.substr(0, pos);
        } else {
            return text;
        }
    }

    function openInBackgroundTab(url){
        var a = document.createElement("a");
        a.href = url;
        var evt = document.createEvent("MouseEvents");
        //the tenth parameter of initMouseEvent sets ctrl key
        evt.initMouseEvent("click", true, true, window, 0, 0, 0, 0, 0,
                                    true, false, false, false, 0, null);
        a.dispatchEvent(evt);
    }

    function check_expense_date() {
        var control = $("[name$='$theControl$dtDate']");
        var date = new Date($(control).val());
        var expiry = new Date((new Date()).getTime() - <%= Settings("Expire") %>*24*3600000); //Number of days is set in Reimbursement settings
        if (date < expiry) {
            control.addClass("old_date");
            $("span#olddatetext").html("<-- <%= Translate("OldDate").Replace("[DAYS]",Settings("Expire")) %>");
        } else {
            control.removeClass("old_date");
            $("span#olddatetext").html("");
        }

    }

    function loadVendorIds() {
        var company = $("#<%= ddlCompany.ClientID %>").val();
        $.ajax({
            url:"/DesktopModules/AgapeConnect/StaffRmb/WebService.asmx/GetVendorIds",
            data:"{ 'company':'"+company+"'}",
            dataType: "json",
            type: "POST",
            contentType: "application/json; charset=utf-8",
            success: function(data) {
                var vendors = ($.map(data.d, function (item) {
                    return {
                        label: item,
                        value: item.split(']')[0].replace("[","")
                    };
                }));
                $("#<%= tbVendorId.ClientID %>").prop('disabled', false).prop('class', 'autocomplete');
                $("#<%= tbVendorId.ClientID%>").autocomplete({
                    source:  vendors,
                    select: function(event, ui) {
                        $('#<%= tbVendorId.ClientID%>').val(ui.item.value);
                        __doPostBack('<%= tbVendorId.ClientID %>', '');
                    },
                    minLength: 2
                });               
            },
            error: function(a, b, c) {
                console.error('failure: '+a.responseText);
                $("#<%= tbVendorId.ClientID %>").prop('disabled', true);
                $("#<%= tbVendorId.ClientID%>").autocomplete({
                    source: ""
                });

            }
        });

    }

    function finance_tree_click(menu_item) {
        $(".currently_selected").removeClass("currently_selected");
        $(menu_item).addClass("currently_selected").addClass("visited");
    }

    function more_info_clicked(checkbox) {
        if ($(checkbox).is(':checked')) {
            $(".currently_selected").addClass("blue_highlight");
        } else {
            $(".currently_selected").removeClass("blue_highlight");
        }
    }

    function calculate_remaining_balance() {
        var result = "";
        var accBal = $("input[id$='StaffRmb_hfAccountBalance']:first").val();
        var formTot = $("span[id$='GridView1_lblTotalAmount']:last").text().replace("$","");
        if ((accBal === "") || (formTot === "")) {
            result = "";
        } else {
            result = format_money(accBal - formTot);
        }
        $("span[id$='GridView1_lblRemainingBalanceAmount']:last").text(result);
    }

    function updateClearingTotal() {
        var total=0;
        $('.clearAdvance').each(function() {
            total+=Number($(this).val());
        });
        $("span[id$='gvUnclearedAdvances_lblClearAdvanceTotal']").text(total.toFixed(2));
    }

    function updatePerDiem(control, enabled) {
        control.attr('disabled', !enabled);
        if (enabled) {
            control.removeClass('aspNetDisabled');
            if (control.hasClass('pdbreakfast') && control.val()===0) { control.val($("[id$='hfBreakfast']").val()); }
            if (control.hasClass('pdlunch') && control.val()===0) { control.val($("[id$='hfLunch']").val()); }
            if (control.hasClass('pdsupper') && control.val()===0) { control.val($("[id$='hfSupper']").val()); }
        } else {
            control.addClass('aspNetDisabled');
            control.text="0";
        }
        calculatePerDiemTotal();
    }

    function calculatePerDiemTotal() {
        var total = 0;
        if ($('.pdbreakfast').is(':enabled')) { total += parseFloat($('.pdbreakfast').val().replace(',','')); }
        if ($('.pdlunch').is(':enabled')) { total += parseFloat($('.pdlunch').val().replace(',','')); }
        if ($('.pdsupper').is(':enabled')) { total += parseFloat($('.pdsupper').val().replace(',','')); }
        $('#tbPDTotal').text(total.toFixed(2));
    }

    function disableSubmitOnEnter(e)
    {
        var key;      
        if(window.event) { key = window.event.keyCode; } //IE
        else { key = e.which; } //firefox      
        return (key != 13);
    }

    function format_money(n) {
        var decPlaces = 2;
        var decSeparator = '.';
        var thouSeparator = ',';
        var sign = n < 0 ? "-" : "";
        var i = parseInt(n = Math.abs(+n || 0).toFixed(decPlaces), 10) + "";
        var j = (j = i.length) > 3 ? j % 3 : 0;
        return sign + (j ? i.substr(0, j) + thouSeparator : "") + i.substr(j).replace(/(\d{3})(?=\d)/g, "$1" + thouSeparator) + (decPlaces ? decSeparator + Math.abs(n - i).toFixed(decPlaces).slice(2) : "");
    }
 
    function loadAllSubmittedTree() {
        var total_submitted = 0;
        $.getJSON(
            "/DesktopModules/AgapeConnect/StaffRmb/WebService.asmx/AllRmbs?portalid="+$('#<%= hfPortalId.ClientID %>').val()+
                        "&tabmoduleid="+$('#<%= hfTabModuleId.ClientID %>').val()+"&status=<%= StaffRmb.RmbStatus.Submitted%>",
            function(data) {
                $("#treeSubmitted").tree({
                    data: data,
                    onCreateLi: function(node, $li) {
                        $li.find('.jqtree-title').not('.jqtree-title-folder').addClass('menu_link');
                        if (node.rmbno) {
                            total_submitted++;
                        }
                    }
                });
                $("#treeSubmitted").bind(
                    'tree.click',
                    function(event) {
                        var node = event.node;
                        if (node.rmbno) {
                            loadRmb(node.rmbno);
                        } else {
                            $("#treeSubmitted").tree('toggle', node);
                        }
                    });
            }
            ).done(function() {
                <%If IsAccounts() Then%>
                $('#treeSubmitted span.jqtree-title:first').text("All Staff ("+total_submitted+")");
                <%End If%>
            }
        );
    }

    function loadAllProcessingTree() {
        var total_processing = 0;
        $.getJSON(
            "/DesktopModules/AgapeConnect/StaffRmb/WebService.asmx/AllRmbs?portalid="+$('#<%= hfPortalId.ClientID %>').val()+
                        "&tabmoduleid="+$('#<%= hfTabModuleId.ClientID %>').val()+"&status=<%= StaffRmb.RmbStatus.Processing%>",
            function(data) {
                $("#treeProcessing").tree({
                    data: data,
                    onCreateLi: function(node, $li) {
                        $li.find('.jqtree-title').not('.jqtree-title-folder').addClass('menu_link');
                        if (node.rmbno) {
                            total_processing++;
                        }
                    }
                });
                $("#treeProcessing").bind(
                    'tree.click',
                    function(event) {
                        var node = event.node;
                        if (node.rmbno) {
                            loadRmb(node.rmbno);
                        } else {
                            $("#treeProcessing").tree('toggle', node);
                        }
                    });
            }
            ).done(function() {
                <%If IsAccounts() Then%>
                $('#lblProcessing').text('(' + total_processing + ')');
                $('#treeProcessing span.jqtree-title:first').text("All Staff ("+total_processing+")");
                <%End If%>
            }
        );
    }

    function loadAllPaidTree() {
        var total_paid=0;
        $.getJSON(
            "/DesktopModules/AgapeConnect/StaffRmb/WebService.asmx/AllRmbs?portalid="+$('#<%= hfPortalId.ClientID %>').val()+
                        "&tabmoduleid="+$('#<%= hfTabModuleId.ClientID %>').val()+"&status=<%= StaffRmb.RmbStatus.Paid%>",
            function(data) {
                $("#treePaid").tree({
                    data: data,
                    onCreateLi: function(node, $li) {
                        $li.find('.jqtree-title').not('.jqtree-title-folder').addClass('menu_link');
                        if (node.rmbno) {
                            total_paid++;
                        }
                    }
                });
                $("#treePaid").bind(
                    'tree.click',
                    function(event) {
                        var node = event.node;
                        if (node.rmbno) {
                            loadRmb(node.rmbno);
                        } else {
                            $("#treePaid").tree('toggle', node);
                        }
                    });
            }
            ).done(function() {
                <%If IsAccounts() Then%>
                $('#treePaid span.jqtree-title:first').text("All Staff ("+total_paid+")");
                <%End If%>
            }
        );
    }

    (function ($, Sys) {

        function setUpMyTabs() {
            var stop = false;

            // If a foreign currency is selected...
            if ($('.ddlCur :selected').val() != "<%=StaffBrokerFunctions.GetSetting("AccountingCurrency", PortalId)%>") {
                //if ($(".exchangeRate").val() == Null || $('.exchangeRate').val() == 0) {
                //    $(".exchangeRate").val("1.0000");
                //    $(".equivalentCAD").val($(".rmbAmount").val());
                //}

                // Disable the amount field; it will get calculated automatically
                //$('.rmbAmount').prop('disabled', true);
                // Set the foreign currency amount to whatever the original amount is
                //TODO $('.foreignCurrency').val($('.rmbAmount').val());
                // Re-calculate the exchange rate
                // We have foreign currency
                $('#<%= hfCurOpen.ClientID %>').val("true");
            }

            $('.ddlReceipt').change(function() { 
                
                if( $('#' + this.id).val() == 2){
                   
                    $("#<%= pnlElecReceipts.ClientID%>").slideDown("slow");
                }
                else{
                    $("#<%= pnlElecReceipts.ClientID%>").slideUp("slow");
                }
            });


            $("#accordion h3").click(function (event) {
                if (stop) {
                    event.stopImmediatePropagation();
                    event.preventDefault();
                    stop = false;
                }
            });

            $("#divWarningDialog").dialog({
                autoOpen: false,
                position: ['middle', 230],
                height: 240,
                width: 500,
                modal: true,
                draggable: false,
                dialogClass: 'draggable',
                close: function () {}
            });
            $("#divWarningDialog").parent().appendTo($("form:first"));

            $("#divClearAdvancePopup").dialog({
                autoOpen: false,
                width: 600,
                position: ['middle', 230],
                modal: true,
                draggable: false,
                dialogClass: 'draggable',
                title: '<%= Translate("ClearAdvance") %>',
                close: function () {
                    // allFields.val("").removeClass("ui-state-error");
                }
            });
            $("#divClearAdvancePopup").parent().appendTo($("form:first"));
            
            $("#divSplitPopup").dialog({
                autoOpen: false,
                height: 400,
                width: 500,
                position: ['middle', 230],
                modal: true,
                draggable: false,
                dialogClass: 'draggable',
                title: '<%= Translate("SplitTransaction") %>',
                close: function () {
                    // allFields.val("").removeClass("ui-state-error");
                }
            });
            $("#divSplitPopup").parent().appendTo($("form:first"));

            $("#divNewItem").dialog({
                autoOpen: false,
                position:['middle', 120],
                width: 780,
                modal: true,
                draggable: false,
                dialogClass: 'draggable',
                title: '<%= Translate("AddEditRmb") %>',
                close: function () {
                    //  allFields.val("").removeClass("ui-state-error");
                }
            });
            $("#divNewItem").parent().appendTo($("form:first"));

            $("#divNewRmb").dialog({
                autoOpen: false,
                position:['middle', 150],
                width: 500,
                modal: true,
                draggable: false,
                dialogClass: 'draggable',
                title: '<%= Translate("CreateRmb") %>',
                close: function () {
                    //  allFields.val("").removeClass("ui-state-error");
                }
            });
            $("#divNewRmb").parent().appendTo($("form:first"));

            $("#divInsufficientFunds").dialog({
                autoOpen: false,
                position:['middle', 150],
                height: 400,
                width: 600,
                modal: true,
                draggable: false,
                dialogClass: 'draggable',
                close: function () {
                    // allFields.val("").removeClass("ui-state-error");
                }
            });
            $("#divInsufficientFunds").parent().appendTo($("form:first"));

            $("#divAccountWarning").dialog({
                autoOpen: false,
                position:['middle', 150],
                height: 150,
                width: 500,
                modal: true,
                draggable: false,
                dialogClass: 'draggable',
                title: '<%= Translate("AccountWarning")%>',
                close: function () {
                    //  allFields.val("").removeClass("ui-state-error");
                }
            });
            $("#divAccountWarning").parent().appendTo($("form:first"));

            $("#divGetPostingData").dialog({
                autoOpen: false,
                position:['middle', 275],
                width: 500,
                modal:true,
                draggable:false,
                title: '<%= Translate("GetPostingDetails") %>',
                dialogClass:'draggable',
                close: function() {}
            });
            $("#divGetPostingData").parent().appendTo($("form:first"));

            $("#divSuggestedPayments").dialog({
                autoOpen: false,
                position:['middle', 150],
                height: 235,
                width: 625,
                modal: true,
                draggable: false,
                dialogClass: 'draggable',
                title: '<%= Translate("SuggestedPayments") %>',
                close: function () {
                    //  allFields.val("").removeClass("ui-state-error");
                }
            });
            $("#divSuggestedPayments").parent().appendTo($("form:first"));

            $('.aButton').button();
            $('.Excel').button({ icons: { primary: 'myExcel'} });


            if (!Modernizr.inputtypes.date) {
                $('input[type=date]').datepicker({
                    dateFormat: 'yy-mm-dd'
                });
            }
            $('.Description').Watermark('<%= Translate("Description") %>');
            $('.Amount').Watermark('<%= Translate("Amount") %>');
        }


        function setUpAccordion() {
            $("#accordion").accordion({
                header: "> div > h3",
                active: <%= getSelectedTab() %>,
                navigate: false
            });
        }

        function checkForMinistryAccount() {
            var account = $("#<%= tbChargeTo.ClientID %>").val();
            if (! account) {
                isMinistryAccount = false;
            } else {
                isMinistryAccount = (account.charAt(0)!='8' && account.charAt(0)!='9');
            }
        }

        function setUpReceiptPreviews() {
            var url = "";
            $(".viewReceipt").hover(function(e){
                var html;
                // Force IE to reload image every time, to keep up with any rotations
                if (window.navigator.userAgent.indexOf("MSIE ") > 0 || !!navigator.userAgent.match(/Trident.*rv\:11\./)) {
                    html = "<div id='preview' style='position:fixed; top:300px; right:25px'><img src='"+this.id+"&r="+new Date().getTime()+"' alt='Missing Receipt Image' style='width:250px'/></div>";
                }
                else { // Not IE
                    html = "<div id='preview' style='position:fixed; top:300px; right:25px'><img src='"+this.id+"' alt='Missing Receipt Image' style='width:250px'/></div>";
                }
                $("body").append(html);
                $("#preview").fadeIn("fast");
            },function(){
                $("#preview").remove();
            });
            $(".multiReceipt").hover(function(e){
                $("body").append("<div id='multi_notify' style='position:fixed; bottom:300px; right:50px'><center></br></hr><span class='AgapeH2'><%=Translate("MultipleReceipts")%></span></br><%=Translate("EditToView")%></center>");
                $("#multi_notify").show();
            },function(){
                $("#multi_notify").remove();
            });
            
        }

        function loadFinanceTrees() {
            loadAllSubmittedTree();
            loadAllProcessingTree();
            loadAllPaidTree();
        }

        function enableDraggable() {
            $('.draggable').draggable({disabled:false});
        }

        function setUpConfirms() {
            $('.confirm').click(function() {
                return window.confirm("Are you sure?");
            });
        }

        function setUpNumbers() {
            $("input[type='number']").click(function () {
                $(this).select();
            });
        }

        function setUpHelpLink() {
            $("#help-link").attr("href", "https://wiki.powertochange.org/help/index.php/Online_Reimbursements").attr("target", "_blank");        
        }

        $(document).ready(function () {
            setUpMyTabs();
            setUpAutocomplete();
            setUpAccordion();
            checkForMinistryAccount();
            loadFinanceTrees();
            setUpNumbers();
            setUpConfirms();
            setUpHelpLink();
                         

            Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
                setUpMyTabs();
                setUpAutocomplete();
                checkForMinistryAccount();
                setUpReceiptPreviews();
                enableDraggable();
                setUpNumbers();
                setUpConfirms();
            });


        });


    } (jQuery, window.Sys));

function GetAccountBalance(jsonQuery){
    $.getJSON(jsonQuery, function(json){
        var amountString = '<%=StaffBrokerFunctions.GetSetting("Currency", PortalId)  %>' + json;
        $("#<%= lblAccountBalance.ClientId %>").html(amountString);
        $("#<%= hlpAccountBalance.ClientId %>").text("<%=Translate("lblAccountBalance.Help") %>");
    });

}

 function expandReceipt(){
     $("#<%= ifReceipt.ClientID %>").show();
 }

 function closeNewItemPopup()  {$("#divNewItem").dialog("close");}
 function closeNewRmbPopup() {$("#divNewRmb").dialog("close");}
 function closeNSFPopup() {$("#divInsufficientFunds").dialog("close");}
 function closeClearAdvancePopup() {$("#divClearAdvancePopup").dialog("close"); $('#loading').hide();}
 function closePopupSplit() {$("#divSplitPopup").dialog("close"); $("#loading").hide();}
 function closeWarningDialog() {$("#divWarningDialog").dialog("close");}
 function closePopupAccountWarning() {$("#divAccountWarning").dialog("close");}
 function closeSuggestedPayments() {$("#divSuggestedPayments").dialog("close");}
 function closePostDataDialog() {$("#divGetPostingData").dialog("close"); $("#loading").hide();}

 function selectIndex(tabIndex) {
     $("#accordion").accordion("option", "active", tabIndex);        
     return false;
 }

 function enableAddLine() {
     document.getElementById("addLinebtn").disabled = "";
     return False;
 }
 function disableAddLine() {
     document.getElementById("addLinebtn").disabled = "disabled";
     return False;
 }
 function resetNewRmbPopup() {
     $('#<%= hfNewChargeTo.ClientID%>').val(''); 
     $('#<%= tbNewChargeTo.ClientID%>').val(''); 
     $('#<%= tbNewYourRef.ClientID%>').val('');
     $('#<%= tbNewComments.ClientID%>').val('');
     $('#<%= tbNewOnBehalfOf.ClientID%>').val('');
     $('#<%= hfOnBehalfOf.ClientID%>').val('');
     if (!Modernizr.inputtypes.date) {
         $('input[type=date]').datepicker({
             dateFormat: 'yy-mm-dd'
         });
     }
 }

    function resetSplitPopup() {
        $('#<%= btnOK.ClientID%>').prop('disabled', true);
        $('#<%= tblSplit.ClientID%>').find('tr:gt(0)').remove();
        $('#<%= tblSplit.ClientID%>').find('input').val('');
    }
    
 function showNewLinePopup()  {$("#divNewItem").dialog("open"); return false;}
 function showNewRmbPopup() {resetNewRmbPopup(); $("#divNewRmb").dialog("open"); return false; }
 function showNSFPopup() {$("#divInsufficientFunds").dialog("open"); return false; }
 function showClearAdvancePopup() { updateClearingTotal(); $("#divClearAdvancePopup").dialog("open"); return false; }
 function showPopupSplit() {resetSplitPopup(); $("#divSplitPopup").dialog("open"); return false; }
 function showWarningDialog() {$("#divWarningDialog").dialog("open"); return false; }
 function showAccountWarning() { $("#divAccountWarning").dialog("open"); return false; }
 function showPostDataDialog() { 
     if ($('.FormTotal').text().replace('$','') < 0) {
         $('<div></div>').appendTo('body')
             .html('<div><%=Translate("ConfirmNegativeReimbursement")%></div>')
             .dialog({
                 modal: true,
                 title: 'Negative Reimbursement',
                 zIndex: 10000,
                 autoOpen: true,
                 width: '500px',
                 resizable: false,
                 buttons: {
                     Yes: function () {
                         $("#divGetPostingData").dialog("open"); 
                         $(this).dialog("close");
                     },
                     No: function () {
                         $(this).dialog("close");
                     }
                 },
                 close: function (event, ui) {
                     $(this).remove();
                 }
             });
     } else {
         $("#divGetPostingData").dialog("open"); 
     }
     return false;
 }

     
 function showSuggestedPayments() {
      
     $('#ifSugPay').attr('src','https://www.youtube.com/embed/PEaTnZrpxfs?rel=0&wmode=transparent');
      
     $("#divSuggestedPayments").dialog("open"); 
     return false;

 }

    //********StaffRmbControl functions**************
    function format_number(item, places) {
        $(item).val(Number($(item).val()).toFixed(places));
    }

    function update_CAD() {
        // update the equivalent CAD based on the (foreign) amount and exchange rate
        console.log('update_CAD()');
        var exchange_rate = parseFloat($('.exchangeRate').val().replace(',',''));
        if (exchange_rate===0) {
            exchange_rate=1;
            $('.exchangeRate').val('1.0000');
            console.log('0 exchange rate; set to 1');
        }
        var amount = parseFloat($('.rmbAmount').val().replace(',',''));
        if (amount===0) {
            $('.rmbAmount').val('0.00');
        }
        var CAD = Number(amount / exchange_rate).toFixed(2);
        $('.equivalentCAD').val(CAD);
        $("input[name$='hfCADValue']").val(CAD); //TODO:Get rid of this - use .equivalentCAD instead
        check_if_receipt_is_required();
        console.log('--Equivalent CAD set to: '+CAD);
    }

    function adjust_exchange_rate() {
        // adjust the exchange rate based on foreign and CAD amounts
        console.log('adjust_exchange_rate()');
        var amount = parseFloat($('.rmbAmount').val().replace(',',''));
        var CAD = parseFloat($('.equivalentCAD').val().replace(',',''));
        var exchange_rate = 0;
        if (CAD <=0 ) {
            $('.equivalentCAD').val($('.rmbAmount').val());
            exchange_rate = '1.0000';
            console.log('ERROR: equivalent CAD was <= 0');
        } else {
            exchange_rate = Number(amount / CAD).toFixed(4);
        }
        $('.exchangeRate').val(exchange_rate);
        console.log('--exchange rate set to: '+ exchange_rate + ' (' + amount + "/" + CAD + ')');
    }

    function display_foreign_exchange() {
        //show the foreign exchange details box if the currency is not the local currency
        console.log('display_foreign_exchange()');
        var local_currency = $("input[name$='hfAccountingCurrency']").val();
        var selected_currency = $('.ddlCur').val();
        var action;
        $("[name$='hfOrigCurrency']").val(selected_currency); //TODO: get rid of hfOrigCurrency
        $('.hfCurOpen').val(selected_currency != local_currency); //TODO: get rid of hfCurOpen
        if (selected_currency == local_currency) {
            $('.curDetails').hide();
            action='hide()';
        } else {
            $('.curDetails').show();
            action='show()';
        }
        console.log('--currency: '+selected_currency + " - " + action);
    }

    function check_if_receipt_is_required() {
        // determine whether the "no receipt" option should be enabled
        console.log('check_if_receipt_is_required()');
        var limit = parseFloat($("#<%= hfNoReceiptLimit.ClientID%>").attr('value').replace(',',''));
        var amount = parseFloat($("input.equivalentCAD").val().replace(',',''));
        var disabled=false;
        try {
            if (amount > limit) {
                if ($('.ddlReceipt').val() == '<%=StaffRmb.RmbReceiptType.No_Receipt %>') {
                    $('.ddlReceipt').val(<%=StaffRmb.RmbReceiptType.Standard %>);
                }
                disabled=true;
            }
            $('.ddlReceipt option[value="<%=StaffRmb.RmbReceiptType.No_Receipt%>"]').prop('disabled', disabled);
        } catch (err) { }
        console.log('-- '+disabled);
    }

    //***********************************************


    function calculateTotal() {
        var total = 0.00;

        $(".Amount").each(function() {
            if (!isNaN(this.value) && this.value.length !== 0) {total += parseFloat(this.value.replace(',',''));}
        });
       
        var orig = $("#<%= lblOriginalAmt.ClientId %>").html();

        if(total== parseFloat(orig.substring(0,orig.Length).replace(',','')))
        {
            $("#<%= btnOK.ClientId %>").prop('disabled', false).removeClass('aspNetDisabled');
        }
        else
        {
            $("#<%= btnOK.ClientId %>").prop('disabled', true).addClass('aspNetDisabled');
        }
    }

    function setUpAutocomplete() {
        var cache = {};
        var usercache = {};
        $("#<%= tbChargeTo.ClientID%>").autocomplete({
            source:  function(request, response) {
                var term = request.term;
                if (term in cache) {
                    console.info('accounts list from cache');
                    response(cache[term]);
                    return;
                }
                console.info('looking up accounts list');
                $.ajax({
                    url:"/DesktopModules/AgapeConnect/StaffRmb/WebService.asmx/GetAccountNumbers",
                    dataType: "json",
                    data: {term: term},
                    type: "POST",
                    success: function(data) {
                        cache[term] = data;
                        response(data);
                    },
                    error: function(a, b, c) {
                        console.error('failure :'+b);
                    }
                });
            },
            select: function(event, ui) {
                console.debug("SELECT: "+ui.item.value);
                $('#<%= hfChargeToValue.ClientID%>').val(ui.item.value);
                $('#<%= tbChargeTo.ClientID%>').val(ui.item.value).change();
            },
            change: function(event, ui) {
                if (!ui.item) {
                    console.debug("CHANGE: -null-");
                    $('#<%= hfChargeToValue.ClientID%>').val('');
                    $('#<%= tbChargeTo.ClientID%>').val('');
                    alert("Please select an account again.  You must click on an item in the list, rather than just typing it.");
                }
            },
            minLength: 2
        });
        $("#<%= tbNewChargeTo.ClientID%>").autocomplete({
            source:  function(request, response) {
                var term = request.term;
                if (term in cache) {
                    console.info('accounts list from cache');
                    response(cache[term]);
                    return;
                }
                console.info('looking up accounts list');
                $.ajax({
                    url:"/DesktopModules/AgapeConnect/StaffRmb/WebService.asmx/GetAccountNumbers",
                    dataType: "json",
                    data: {term: term},
                    type: "POST",
                    success: function(data) {
                        cache[term] = data;
                        response(data);
                    },
                    error: function(a, b, c) {
                        console.error('failure :'+b);
                    }
                });
            },
            select: function(event, ui) {
                console.debug("SELECT: "+ui.item.value);
                $('#<%= hfNewChargeTo.ClientID%>').val(ui.item.value);
            },

            minLength: 2
        });
        $("#<%= tbCostCenter.ClientID%>").autocomplete({
            source:  function(request, response) {
                var term = request.term;
                if (term in cache) {
                    console.info('accounts list from cache');
                    response(cache[term]);
                    return;
                }
                console.info('looking up accounts list');
                $.ajax({
                    url:"/DesktopModules/AgapeConnect/StaffRmb/WebService.asmx/GetAccountNumbers",
                    dataType: "json",
                    data: {term: term},
                    type: "POST",
                    success: function(data) {
                        cache[term] = data;
                        response(data);
                    },
                    error: function(a, b, c) {
                        console.error('failure :'+b);
                    }
                });
            },
            select: function(event, ui) {
                console.debug("SELECT: "+ui.item.value);
                $('#<%= tbCostCenter.ClientID%>').val(ui.item.value);
            },
            change: function(event, ui) {
                if (!ui.item) {
                    console.debug("CHANGE: -null-");
                    $('#<%= tbCostCenter.ClientID%>').val('');
                    alert("Please select an account again.  You must click on an item in the list, rather than just typing it.");
                }
            },
            minLength: 2
        });
        $("#<%= tbNewOnBehalfOf.ClientID%>").autocomplete({
            source:  function(request, response) {
                var term = request.term;
                if (term in usercache) {
                    console.info('users from usercache');
                    response(usercache[term]);
                    return;
                }
                console.info('looking up users');
                $.ajax({
                    url:"/DesktopModules/AgapeConnect/StaffRmb/WebService.asmx/GetStaffNames",
                    dataType: "json",
                    data: {portalid:<%=PortalId%>, term: term},
                    type: "POST",
                    success: function(data) {
                        usercache[term] = data;
                        response(data);
                    },
                    error: function(a, b, c) {
                        console.error('failure :'+b);
                    }
                });
            },
            select: function(event, ui) {
                console.debug("SELECT: "+ui.item.value);
                event.preventDefault();
                $('#<%= hfOnBehalfOf.ClientID%>').val(ui.item.value);
                $('#<%= tbNewOnBehalfOf.ClientID%>').val(ui.item.label).change();
            },
            change: function(event, ui) {
                if (!ui.item) {
                    console.debug("CHANGE: -null-");
                    $('#<%= hfOnBehalfOf.ClientID%>').val('');
                    $('#<%= tbNewOnBehalfOf.ClientID%>').val('');
                    alert("Please select the staff member again.  You must click on a name in the list, rather than just typing it.");
                }
            },
            minLength: 2
        });

    }

    function show_loading_spinner() {
        $("#loading").show();
        return true;
    }

    function showSaveButton() {
        $('#<%=btnSave.ClientId%>').prop('value', '<%=Translate("btnSave")%>');
        $('#<%=btnSave.ClientID%>').show();
    }

    var isMinistryAccount = false;

</script>
<style type="text/css">
    .AgapeWarning {
        display: block;
        margin-bottom: 5px;
        padding: 3px;
    }

    .myExcel {
        width: 16px;
        height: 16px;
        background-image: url('/DesktopModules/AgapeConnect/StaffRmb/Images/Excel_icon.gif') !important;
    }

    .hdrTitle {
        white-space: nowrap;
        color: Gray;
    }

    .hdrValue {
    }

    .AcPane {
        height: 280px;
    }
</style>

<div id="loading" class="loading_overlay" style="display:none" >
    &nbsp;
</div>
<asp:Label ID="lblVersion" runat="server" CssClass="left hint" style="margin-top:-50px; margin-left:250px" />
<div style="position:relative; text-align: center; width: 100%;">
    <asp:UpdatePanel ID="ErrorUpdatePanel" runat="server" >
        <ContentTemplate>
            <asp:Label ID="lblError" runat="server" class="ui-state-error ui-corner-all"
                Style="padding: 3px; margin-top: 3px; display: inline-block; width: 50%;" Visible="false"></asp:Label>
        </ContentTemplate>
    </asp:UpdatePanel>
</div>
<asp:Panel ID="pnlEverything" runat="server" style="position:relative;">


    <div style="padding-bottom: 5px;">
        <asp:Label ID="Label2" runat="server" CssClass="AgapeH2" resourcekey="RmbTitle" Visible="false"></asp:Label>
    </div>
    <asp:HiddenField ID="hfNoReceiptLimit" runat="server" Value="0" />
    <asp:HiddenField ID="hfPortalId" runat="server" Value="-1" />
    <asp:HiddenField ID="hfAccountingCurrency" runat="server" Value="USD" />
    <asp:HiddenField ID="hfExchangeRate" runat="server" Value="1" />
    <asp:HiddenField ID="hfOrigCurrency" runat="server" Value="" />
    <asp:HiddenField ID="hfOrigCurrencyValue" runat="server" Value="" />
    <asp:HiddenField ID="hfCurOpen" runat="server" Value="false" />
    <asp:HiddenField ID="hfChargeToValue" runat="server"  />
    <asp:HiddenField ID="hfTabModuleId" runat="server" Value="-1" />
	<a target="_blank" style="position: relative; float: right; right: 50px; top: -50px;" href="https://wiki.powertochange.org/help/index.php/Online_Reimbursements">Help</a>

    <table width="100%">
        <tr valign="top">
            <td>
                <div align="center" width="100%">
                    <input id="btnNewRmb" type="button" onclick="showNewRmbPopup();" class="aButton" value='<%= Translate("btnNew") %>'/>
                </div>
                <div id="accordion">
                    <div style="text-align:center">
                        <asp:Label ID="lblTeamLeader" runat="server" Class="buttonLabel" resourcekey="TeamLeader" Visible="false" />
                        <asp:Label ID="lblAccountsTeam" runat="server" class="buttonLabel" resourcekey="AccountsMode" Visible="false" />
                    </div>

                    <div>
                        <h3>
                            <a href="#" id="Tab0" class="AcHdr">
                                <asp:Label ID="Label5" runat="server" Font-Bold="true" ResourceKey="Draft"></asp:Label></a></h3>
                        <div id="DraftPane" class="AcPane">
                            <asp:UpdatePanel ID="DraftsUpdatePanel" runat="server" ChildrenAsTriggers="false" UpdateMode="Conditional" >
                                <ContentTemplate>
                                    <asp:Label ID="lblErrors" runat="server" class="ui-state-error ui-corner-all"
                                        Style="padding: 3px; margin-top: 3px; display: block;" Visible="false"></asp:Label>
                                    <asp:DataList ID="dlPending" runat="server" Width="100%">
                                        <ItemStyle CssClass="dnnGridItem" />
                                        <AlternatingItemStyle CssClass="dnnGridAltItem" />
                                        <ItemTemplate>

                                            <table width="100%">
                                                <tr valign="middle">
                                                    <td>
                                                        <asp:Image ID="Image2" runat="server" Width="35px" ImageUrl='<%# GetProfileImage(Eval("UserId")) %>' />
                                                    </td>
                                                    <td width="100%" align="left">
                                                        <asp:LinkButton ID="LinkButton" runat="server" OnClientClick='selectMenuItem(this);' Text='<%# GetRmbTitle(Eval("UserRef"), Eval("RID"), Eval("RmbDate"))  %>'  CommandArgument='<%# Eval("RmbNo") %>' CommandName="Goto" 
                                                            Font-Size='<%# If(IsSelected(Eval("RmbNo")), "9", "10")%>' Font-Bold='<%# IsSelected(Eval("RmbNo")) %>' Width="100%"></asp:LinkButton>
                                                    </td>
                                                    <td>
                                                        <img ID="Image1" runat="server" alt=">" src="~/images/action_right.gif" style='<%# "visibility:" & if(IsSelected(Eval("RmbNo")), "visible", "hidden") & "; margin:-6px" %>' />
                                                    </td>
                                                </tr>
                                            </table>
                                        </ItemTemplate>
                                    </asp:DataList>
                                </ContentTemplate>
                            </asp:UpdatePanel>
                        </div>
                    </div>
                    <div>
                        <h3>
                                <asp:UpdatePanel ID="pnlSubmitted" runat="server" UpdateMode="Conditional">
                                    <ContentTemplate>
                                        <a href="#" id="Tab1" class="AcHdr">
                                        <asp:Label ID="Label6" runat="server" Font-Bold="true" ResourceKey="Submitted"></asp:Label>&nbsp;<asp:Label ID="lblSubmittedCount" runat="server" Font-Bold="true"></asp:Label></a>
                                    </ContentTemplate>
                                </asp:UpdatePanel></h3>
                            <div id="SubmittedPane" class="AcPane">
                                <asp:UpdatePanel ID="SubmittedUpdatePanel" runat="server" ChildrenAsTriggers="false" UpdateMode="Conditional" >
                                    <ContentTemplate>
                                        <asp:Placeholder id="submittedPlaceholder" runat="server"></asp:Placeholder>
                                        <asp:Panel ID="pnlSubmittedView" runat="server">
                                            <asp:Label ID="lblApproveHeading" runat="server" class="approver" ResourceKey="RmbsToApprove" Style="font-size: 8pt;"></asp:Label>
                                            <asp:DataList ID="dlToApprove" runat="server" Width="100%">
                                                <ItemStyle CssClass="dnnGridItem" />
                                                <AlternatingItemStyle CssClass="dnnGridAltItem" />
                                                <ItemTemplate>
                                                    <table width="100%">
                                                        <tr valign="middle">
                                                            <td>
                                                                <asp:Image ID="Image2" runat="server" ImageUrl='<%# GetProfileImage(Eval("UserId")) %>' Width="35px" />
                                                            </td>
                                                            <td align="left" width="100%">
                                                                <asp:LinkButton ID="LinkButton" runat="server" OnClientClick='selectMenuItem(this);' CommandArgument='<%# Eval("RmbNo") %>' CommandName="Goto" Text='<%# GetRmbTitleTeam(Eval("RID"), Eval("UserId"), Eval("RmbDate"))  %>' 
                                                                    Font-Size='<%# If(IsSelected(Eval("RmbNo")), "9", "10")%>' Font-Bold='<%# IsSelected(Eval("RmbNo")) %>' Width="100%"></asp:LinkButton>
                                                            </td>
                                                            <td>
                                                                <img ID="Image1" runat="server" alt=">" src="~/images/action_right.gif" style='<%# "visibility:" & if(IsSelected(Eval("RmbNo")), "visible", "hidden") & "; margin:-6px" %>' />
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </ItemTemplate>
                                            </asp:DataList>
                                            <asp:Label ID="lblSubmitted" runat="server" class="my_section" ResourceKey="YourRmbs" Style="font-size: 8pt;"></asp:Label>
                                            <asp:DataList ID="dlSubmitted" runat="server" Width="100%">
                                                <ItemStyle CssClass="dnnGridItem" />
                                                <AlternatingItemStyle CssClass="dnnGridAltItem" />
                                                <ItemTemplate>
                                                    <table width="100%">
                                                        <tr valign="middle">
                                                            <td>
                                                                <asp:Image ID="Image2" runat="server" ImageUrl='<%# GetProfileImage(Eval("UserId")) %>' Width="35px" />
                                                            </td>
                                                            <td align="left" width="100%">
                                                                <asp:LinkButton ID="LinkButton" runat="server" OnClientClick='selectMenuItem(this);' CommandArgument='<%# Eval("RmbNo") %>' CommandName="Goto" Text='<%# GetRmbTitle(Eval("UserRef"), Eval("RID"), Eval("RmbDate"))  %>'
                                                                    Font-Size='<%# If(IsSelected(Eval("RmbNo")), "9", "10")%>' Font-Bold='<%# IsSelected(Eval("RmbNo")) %>' Width="100%"></asp:LinkButton>
                                                            </td>
                                                            <td>
                                                                <img ID="Image1" runat="server" alt=">" src="~/images/action_right.gif" style='<%# "visibility:" & if(IsSelected(Eval("RmbNo")), "visible", "hidden") & "; margin:-6px" %>' />
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </ItemTemplate>
                                            </asp:DataList>
                                        </asp:Panel>
                                    </ContentTemplate>
                                </asp:UpdatePanel>
                            </div>
                    </div>
                    <div>
                        <h3>                            
                                <asp:UpdatePanel ID="pnlToProcess" runat="server" UpdateMode="Conditional">
                                    <ContentTemplate>
                                        <a href="#" id="Tab2" class="AcHdr">
                                        <asp:Label ID="Label7" runat="server" Font-Bold="true" ResourceKey="Approved"></asp:Label>&nbsp;<asp:Label ID="lblToProcess" runat="server" Font-Bold="true"></asp:Label></a>
                                    </ContentTemplate>
                                </asp:UpdatePanel></h3>
                            <div id="ApprovedPane" class="AcPane">
                                <asp:UpdatePanel ID="ApprovedUpdatePanel" runat="server" ChildrenAsTriggers="false" UpdateMode="Conditional" >
                                    <ContentTemplate>
                                        <asp:TreeView ID="tvTeamApproved" class="team_leader" runat="server" ResourceKey="TeamRmbs" NodeIndent="10">
                                        </asp:TreeView>
                                        <asp:TreeView ID="tvFinance" class="accounts_team" runat="server" NodeIndent="10">
                                        </asp:TreeView>
                                            <asp:Label ID="lblApproved" runat="server" class="my_section" ResourceKey="YourRmbs" Style="font-size: 8pt;">
                                                <br />
                                            </asp:Label>
                                            <asp:DataList ID="dlApproved" runat="server" Width="100%">
                                                <ItemStyle CssClass="dnnGridItem" />
                                                <AlternatingItemStyle CssClass="dnnGridAltItem" />
                                                <ItemTemplate>
                                                    <table width="100%">
                                                        <tr valign="middle">
                                                            <td>
                                                                <asp:Image ID="Image2" runat="server" ImageUrl='<%# GetProfileImage(Eval("UserId")) %>' Width="35px" />
                                                            </td>
                                                            <td align="left" width="100%">
                                                                <asp:LinkButton ID="LinkButton" runat="server" OnClientClick='selectMenuItem(this);' CommandArgument='<%# Eval("RmbNo") %>' CommandName="Goto" Text='<%# GetRmbTitle(Eval("UserRef"), Eval("RID"), Eval("RmbDate"))  %>'  
                                                                    Font-Size='<%# If(IsSelected(Eval("RmbNo")), "9", "10")%>' Font-Bold='<%# IsSelected(Eval("RmbNo")) %>' Width="100%"></asp:LinkButton>
                                                            </td>
                                                            <td>
                                                                <img ID="Image1" runat="server" alt=">" src="~/images/action_right.gif" style='<%# "visibility:" & if(IsSelected(Eval("RmbNo")), "visible", "hidden") & "; margin:-6px" %>' />
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </ItemTemplate>
                                            </asp:DataList>
                                       </ContentTemplate>
                                    </asp:UpdatePanel>             

                            </div>

                    </div>
                    <div>
                        <h3>
                            <a href="#" id="tab3" class="AcHdr">
                                <asp:Label runat="server" Font-Bold="true" ResourceKey="Processing"></asp:Label>&nbsp;<label id="lblProcessing"></label></a></h3>
                       <div id="ProcessingPane" class="AcPane">
                            <asp:UpdatePanel ID="ProcessingUpdatePanel" runat="server" ChildrenAsTriggers="false" UpdateMode="Conditional" >
                                 <ContentTemplate>
                                    <asp:Placeholder id="processingPlaceholder" runat="server"></asp:Placeholder>
                                    <asp:TreeView ID="tvTeamProcessing" class="team_leader" runat="server" NodeIndent="10">
                                    </asp:TreeView>
                                    <asp:Label ID="lblYourProcessing" runat="server" class="my_section" ResourceKey="YourRmbs" Style="font-size: 8pt;">
                                        <br />
                                    </asp:Label>
                                    <asp:DataList ID="dlProcessing" runat="server" Width="100%">
                                        <ItemStyle CssClass="dnnGridItem" />
                                        <AlternatingItemStyle CssClass="dnnGridAltItem" />
                                        <ItemTemplate>
                                            <table width="100%">
                                                <tr valign="middle">
                                                    <td>
                                                        <asp:Image ID="Image2" runat="server" ImageUrl='<%# GetProfileImage(Eval("UserId")) %>' Width="35px" />
                                                    </td>
                                                    <td align="left" width="100%">
                                                        <asp:LinkButton ID="LinkButton" runat="server" OnClientClick='selectMenuItem(this);' CommandArgument='<%# Eval("RmbNo") %>' CommandName="Goto" Text='<%# GetRmbTitle(Eval("UserRef"), Eval("RID"), Eval("RmbDate"))  %>'
                                                            Font-Size='<%# If(IsSelected(Eval("RmbNo")), "9", "10")%>' Font-Bold='<%# IsSelected(Eval("RmbNo")) %>' Width="100%"></asp:LinkButton>
                                                    </td>
                                                    <td>
                                                        <img ID="Image1" runat="server" alt=">" src="~/images/action_right.gif" style='<%# "visibility:" & if(IsSelected(Eval("RmbNo")), "visible", "hidden") & "; margin:-6px" %>' />
                                                    </td>
                                                </tr>
                                            </table>
                                        </ItemTemplate>
                                    </asp:DataList>
                                </ContentTemplate>
                            </asp:UpdatePanel>
                        </div>
                    </div>
                    <div>
                        <h3>
                            <a href="#" id="tab4" class="AcHdr">
                                <asp:Label ID="Label9" runat="server" Font-Bold="true" ResourceKey="Paid"></asp:Label></a></h3>
                        <div id="PaidPane" class="AcPane">
                            <asp:UpdatePanel ID="PaidUpdatePanel" runat="server" ChildrenAsTriggers="false" UpdateMode="Conditional" >
                                <ContentTemplate>
                                    <asp:Placeholder id="paidPlaceholder" runat="server"></asp:Placeholder>
                                    <asp:TreeView ID="tvTeamPaid" class="team_leader" runat="server" NodeIndent="10">
                                    </asp:TreeView>
                                    <asp:Label ID="lblYourPaid" runat="server" class="my_section" ResourceKey="YourRmbs" Style="font-size: 8pt;">
                                        <br />
                                    </asp:Label>
                                    <asp:DataList ID="dlPaid" runat="server" Width="100%">
                                        <ItemStyle CssClass="dnnGridItem" />
                                        <AlternatingItemStyle CssClass="dnnGridAltItem" />
                                        <ItemTemplate>
                                            <table width="100%">
                                                <tr valign="middle">
                                                    <td width="100%">
                                                        <asp:LinkButton ID="LinkButton" runat="server" OnClientClick='selectMenuItem(this);' Text='<%# GetRmbTitle(Eval("UserRef"), Eval("RID"), Eval("RmbDate"))  %>' 
                                                            CommandArgument='<%# Eval("RmbNo") %>' CommandName="Goto" 
                                                            Font-Size='<%# If(IsSelected(Eval("RmbNo")), "9", "10")%>' Font-Bold='<%# IsSelected(Eval("RmbNo")) %>' ></asp:LinkButton>
                                                    </td>
                                                    <td width="10px">
                                                        <img ID="Img1" runat="server" alt=">" src="~/images/action_right.gif" style='<%# "visibility:" & if(IsSelected(Eval("RmbNo")), "visible", "hidden") & "; margin:-6px" %>' />
                                                    </td>
                                                </tr>
                                            </table>
                                        </ItemTemplate>
                                    </asp:DataList>
                                </ContentTemplate>
                            </asp:UpdatePanel>
                        </div>
                        <h3>
                            <a href="#" id="tab5" class="AcHdr">
                                <asp:Label ID="Label50" runat="server" Font-Bold="true" ResourceKey="Cancelled"></asp:Label></a></h3>
                        <div id="CancelledPane" class="AcPane">
                            <asp:UpdatePanel ID="CancelledUpdatePanel" runat="server" ChildrenAsTriggers="false" UpdateMode="Conditional" >
                                <ContentTemplate>
                                    <asp:DataList ID="dlCancelled" runat="server" Width="100%">
                                        <ItemStyle CssClass="dnnGridItem" />
                                        <AlternatingItemStyle CssClass="dnnGridAltItem" />
                                        <ItemTemplate>
                                            <table width="100%">
                                                <tr valign="middle">
                                                    <td width="100%">
                                                        <asp:LinkButton ID="LinkButton" runat="server" OnClientClick='selectMenuItem(this);' Text='<%# GetRmbTitle(Eval("UserRef"), Eval("RID"), Eval("RmbDate"))  %>' 
                                                            CommandArgument='<%# Eval("RmbNo") %>' CommandName="Goto" 
                                                            Font-Size='<%# If(IsSelected(Eval("RmbNo")), "9", "10")%>' Font-Bold='<%# IsSelected(Eval("RmbNo")) %>' ></asp:LinkButton>
                                                    </td>
                                                    <td width="10px">
                                                        <img ID="Img1" runat="server" alt=">" src="~/images/action_right.gif" style='<%# "visibility:" & if(IsSelected(Eval("RmbNo")), "visible", "hidden") & "; margin:-6px" %>' />
                                                    </td>
                                                </tr>
                                            </table>
                                        </ItemTemplate>
                                    </asp:DataList>
                                </ContentTemplate>
                            </asp:UpdatePanel>
                        </div>
                    </div>
                </div>
                <asp:hyperlink ID="hlAdvanceAdjust" runat="server" Text="Advance Adjustment" style="margin-left:50px" Target="_blank"/>
            </td>
            <td width="100%" style="padding-left: 20px;">


                <asp:UpdatePanel ID="splashUpdatePanel" runat="server">
                    <ContentTemplate>
                        <asp:Panel ID="pnlSplash" runat="server" Visible="false">

                            <asp:PlaceHolder ID="MoreInfoPlaceholder" runat="server"></asp:PlaceHolder>

                            <asp:Literal runat="server" ID="ltSplash"></asp:Literal>
                        </asp:Panel>
                    </ContentTemplate>
                </asp:UpdatePanel> 



                <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                    <ContentTemplate>
                        <asp:Panel ID="pnlMain" runat="server" CssClass="ui-widget ui-widget-content ui-corner-all">

                            <div class="ui-accordion-header ui-helper-reset ui-state-default ui-corner-all">
                                <div style="width: 100%; vertical-align: middle; font-size: 20pt; font-weight: bold; border-width: 2pt; border-bottom-style: solid;">
                                    <div style="float:left; width:54px;">&nbsp;</div>
                                    <asp:Label ID="Label17" runat="server" resourcekey="Reimbursement" Style="float: left; margin-right: 5px; margin-left:10px"></asp:Label>
                                    <asp:Label ID="lblRmbNo" runat="server" Style="float: left; margin-right: 5px;"></asp:Label>:
                                    <asp:TextBox ID="tbChargeTo" runat="server" AutoPostBack="true"  Style="float: right; font-size: small;">
                                    </asp:TextBox>
                                </div>
                                <div class="inverse" style="width:100%; margin-top:1px; padding-top:3px; padding-bottom:3px; float: left" >
                                    <asp:Label ID="lblStatus" runat="server" Style="float: left; font-style: italic; font-size:13px; padding-left:70px"></asp:Label>
                                    <div style="float: right; padding-right:10px; margin-right: 3px;">
                                        <div id="accountBalanceDiv">
                                            <asp:Label ID="lblAccountBalance" runat="server" Style="font-style: italic; font-size:13px;"></asp:Label>
                                            <asp:HiddenField ID="hfAccountBalance" runat="server" />
                                            <asp:LinkButton id="lbAccountBalance" TabIndex="-1" runat="server" CausesValidation="False" EnableViewState="False" CssClass="dnnFormHelp"  style="position:relative" Visible="true"/>
                                                <asp:Panel runat="server" CssClass="dnnTooltip">
                                                    <div class="dnnFormHelpContent dnnClear" style="width:500px; left:-400px;">
                                                        <asp:Label ID="hlpAccountBalance" runat="server" class="dnnHelpText" />
                                                        <a href="#" class="pinHelp"></a>
                                                   </div>   
                                                </asp:Panel>
                                        </div>
                                    </div>
                                </div>
                                <asp:Image ID="imgAvatar" runat="server" Width="50px" ImageUrl="/images/no_avatar.gif" Style="float: left; margin-top:-54px; margin-right: 5px; border-width: 2pt; border-style: solid;" />
                                <div style="clear: both;">
                                </div>
                            </div>
                            <div style="margin-top: 10px;" class="rmb_form">
                                <table  class="rmbHeader" width="100%">
                                    <tr class="Agape_SubTitle">
                                        <td class="hdrTitle" width="10%">
                                            <asp:Label ID="Label18" runat="server" resourcekey="SubmittedOn"></asp:Label>
                                        </td>
                                        <td class="hdrValue">
                                            <asp:Label ID="lblSubmittedDate" runat="server"></asp:Label>
                                        </td>
                                        <td class="hdrTitle" width="10%">
                                            <asp:Label ID="Label19" runat="server" resourcekey="ApprovedOn"></asp:Label>
                                        </td>
                                        <td class="hdrValue" width="20%">
                                            <asp:Label ID="lblApprovedDate" runat="server"></asp:Label>
                                        </td>
                                        <td class="hdrTitle" width="10%">
                                            <asp:Label ID="Label20" runat="server" resourcekey="ProcessedOn"></asp:Label>
                                        </td>
                                        <td class="hdrValue" width="20%">
                                            <asp:Label ID="lblProcessedDate" runat="server"></asp:Label>
                                        </td>
                                    </tr>
                                    <tr class="Agape_SubTitle">
                                        <td class="hdrTitle" width="10%">
                                            <asp:Label ID="Label21" runat="server" resourcekey="SubmittedBy"></asp:Label><br />
                                            <b><asp:Label id="lblOnBehalfOf" runat="server" resourcekey="lblOnBehalfOf" Visible="false" /></b>
                                        </td>
                                        <td class="hdrValue">
                                            <asp:Label ID="lblSubBy" runat="server"></asp:Label><br />
                                            <b><asp:Label ID="lblBehalf" runat="server" visible="false"/></b>
                                        </td>
                                        <td style="color: Gray;" width="10%">
                                            <asp:Label ID="ttlWaitingApp" runat="server" resourcekey="AwaitingApproval"></asp:Label>
                                            <asp:Label ID="ttlApprovedBy" runat="server" resourcekey="ApprovedBy" Visible="false"></asp:Label>
                                        </td>
                                        <td class="hdrValue" >
                                            <asp:DropDownList ID="ddlApprovedBy" AutoPostBack="true" runat="server" ></asp:DropDownList>
                                            <asp:Label ID="lblApprovedBy" runat="server" Visible="false"></asp:Label>
                                        </td>
                                        <td class="hdrTitle" width="10%">
                                            <asp:Label ID="Label22" runat="server" resourcekey="ProcessedBy"></asp:Label>
                                        </td>
                                        <td class="hdrValue">
                                            <asp:Label ID="lblProcessedBy" runat="server"></asp:Label>
                                        </td>
                                    </tr>



                                    <tr class="Agape_SubTitle">
                                        <td colspan="2">
                                            <table><tr class="Agape_SubTitle">
                                            <td class="hdrTitle" width="10%">
                                                <asp:Label ID="lblYouRef" runat="server" resourcekey="YourRef"></asp:Label>
                                            </td>
                                            <td class="hdrValue">
                                                <asp:TextBox ID="tbYouRef" runat="server" Width="150px" onKeyPress="showSaveButton();"></asp:TextBox>
                                            </td>
                                            </tr>
                                            </table>
                                        </td>
                                        <td colspan="2">
                                            <asp:Label id="lblExtraApproval" runat="server" cssclass="extra_approval" resourcekey="lblExtraApproval" Visible="false" />
                                        </td>
                                        <td id="pnlPeriodYear" colspan="2" runat="server" style="white-space: nowrap; color: Gray;">
                                            <asp:Label ID="Label24" runat="server" resourcekey="Period"></asp:Label>
                                            <asp:DropDownList ID="ddlPeriod" runat="server" Width="70px" Enabled="false" Font-Size="X-Small">
                                                <asp:ListItem Text="Default" Value="" />
                                                <asp:ListItem Text="Jan" Value="1" />
                                                <asp:ListItem Text="Feb" Value="2" />
                                                <asp:ListItem Text="Mar" Value="3" />
                                                <asp:ListItem Text="Apr" Value="4" />
                                                <asp:ListItem Text="May" Value="5" />
                                                <asp:ListItem Text="Jun" Value="6" />
                                                <asp:ListItem Text="Jul" Value="7" />
                                                <asp:ListItem Text="Aug" Value="8" />
                                                <asp:ListItem Text="Sep" Value="9" />
                                                <asp:ListItem Text="Oct" Value="10" />
                                                <asp:ListItem Text="Nov" Value="11" />
                                                <asp:ListItem Text="Dec" Value="12" />
                                            </asp:DropDownList>
                                            <asp:Label ID="Label25" runat="server" resourcekey="Year"></asp:Label>
                                            <asp:DropDownList ID="ddlYear" runat="server" Width="70px" Font-Size="X-Small" Enabled="false">
                                                <asp:ListItem Text="Default" Value=""></asp:ListItem>

                                            </asp:DropDownList>
                                        </td>
                                    </tr>
                                </table>
                                <table class="rmbHeaderContinuation" width="100%">
                                    <tr valign="top">
                                        <td colspan="2" style="font-size: 8pt; width: 33%;">
                                            <fieldset>
                                                <legend class="AgapeH4">
                                                    <asp:Label ID="ttlYourComments" runat="server" CssClass="hdrTitle" resourcekey="YourComments" Visible="false" /><asp:Label
                                                        ID="ttlUserComments" runat="server" Text="User's Comments" /></legend>
                                                <asp:Label ID="lblComments" runat="server" Height="60px" Style="overflow-y:auto" Visible="false"></asp:Label>
                                                <asp:TextBox ID="tbComments" runat="server" Height="55px" TextMode="MultiLine" Width="100%"
                                                    onKeyPress="showSaveButton();"></asp:TextBox>
                                            </fieldset>
                                        </td>
                                        <td colspan="2" style="font-size: 8pt; width: 33%;">
                                            <fieldset>
                                                <legend class="AgapeH4">
                                                    <asp:Label ID="Label26" runat="server" CssClass="hdrTitle" resourcekey="ApproversComments"></asp:Label></legend>
                                                <asp:Label ID="lblApprComments" runat="server" Height="60px" Style="overflow-y:auto"></asp:Label>
                                                <asp:TextBox ID="tbApprComments" runat="server" Height="35px" TextMode="MultiLine"
                                                    Width="100%" Visible="false"  onKeyPress="showSaveButton();"></asp:TextBox>
                                                <asp:CheckBox ID="cbApprMoreInfo" runat="server" AutoPostBack="true" resourcekey="btnMoreInfo" />
                                            </fieldset>
                                        </td>
                                        <td colspan="2" style="font-size: 8pt; width: 33%;">
                                            <fieldset>
                                                <legend class="AgapeH4">
                                                    <asp:Label ID="Label27" runat="server" CssClass="hdrTitle" resourcekey="AccountsComments"></asp:Label></legend>
                                                <asp:Label ID="lblAccComments" runat="server" Height="60px" Style="overflow-y:auto"></asp:Label>
                                                <asp:TextBox ID="tbAccComments" runat="server" Height="35px" TextMode="MultiLine" Width="100%"
                                                    Visible="false"  onKeyPress="showSaveButton();"></asp:TextBox>
                                                <asp:CheckBox ID="cbMoreInfo" runat="server" AutoPostBack="true" resourcekey="btnMoreInfo" onclick="more_info_clicked(this)" />
                                            </fieldset>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td colspan="4"></td>
                                        <td colspan="2" style="font-size: 8pt; width: 33%;">
                                            <asp:Panel id="pnlPrivateComments" runat="server">
                                                <fieldset>
                                                    <div style="float:right">
                                                        <asp:button id="btnPrivAccComments" runat="server" resourcekey="btnPrivateAccountsComments" />
                                                    </div>
                                                    <legend class="AgapeH4">
                                                        <asp:Label ID="lblPrivAccComments" runat="server" CssClass="hdrTitle privAccComments" resourcekey="PrivateAccountsComments" Visible="false"/>
                                                    </legend>
                                                    <asp:TextBox ID="tbPrivAccComments" runat="server" Height="55px" TextMode="MultiLine" CssClass="privAccComments" Width="100%" visible="false" onKeyPress="showSaveButton();"/>
                                                </fieldset>
                                            </asp:Panel>
                                        </td>
                                    </tr>
                                    <tr valign="top">
                                        <td colspan="6">
                                            <asp:Button ID="btnDelete" runat="server" resourcekey="btnDelete" class="aButton delete" OnClientClick='if (! window.confirm("Are you sure?")) return;' OnClick="btnDelete_Click" style="float:left"/>
                                            <asp:Button ID="btnSave" runat="server" resourcekey="btnSaved" class="aButton" style="float:right"/>
                                        </td>
                                    </tr>
                                </table>
                                <div class="rmbDataLines">
                                    <br />

                                    <asp:Label ID="lblTest" runat="server" Text="Label" Visible="false"></asp:Label>
                                    <div style="padding: 0 20px 0 20px;">
                                        <asp:GridView ID="GridView1" class="rmbDetails" runat="server" AutoGenerateColumns="False" DataKeyNames="RmbLineNo"
                                            CellPadding="4" ForeColor="#333333" GridLines="None" Width="100%" ShowFooter="True" AllowSorting="true" >
                                            <RowStyle CssClass="dnnGridItem" />
                                            <AlternatingRowStyle CssClass="dnnGridAltItem" />
                                            <Columns>
                                                <asp:TemplateField HeaderText="TransDate" SortExpression="TransDate">
                                                    <EditItemTemplate>
                                                        <asp:TextBox ID="TextBox2" runat="server" Text='<%# Bind("TransDate") %>'></asp:TextBox>
                                                    </EditItemTemplate>
                                                    <ItemTemplate>
                                                        <asp:Label ID="Label2" runat="server" CssClass='<%# IIF(Eval("OutOfDate"), "ui-state-highlight highlight ui-corner-all","") %>' ToolTip='<%# IIF(Eval("OutOfDate"),Translate("OutOfDate"),"") %>' Text='<%# Bind("TransDate", "{0:d}") %>'></asp:Label>
                                                    </ItemTemplate>
                                                    <ItemStyle HorizontalAlign="Left" Width="50px" />
                                                </asp:TemplateField>

                                                <asp:TemplateField HeaderText="Extra">
                                                    <EditItemTemplate>
                                                    </EditItemTemplate>
                                                    <ItemTemplate>
                                                        <asp:Label ID="lblExtra" runat="server" Text='<%# Eval("Spare1")%>'></asp:Label>
                                                    </ItemTemplate>
                                                    <ItemStyle HorizontalAlign="Center" />
                                                </asp:TemplateField>

                                                <asp:TemplateField HeaderText="Line Type" ItemStyle-Width="110px">
                                                    <EditItemTemplate>
                                                        <asp:TextBox ID="TextBox1" runat="server" Text='<%# Bind("LineType") %>'></asp:TextBox>
                                                    </EditItemTemplate>
                                                    <ItemTemplate>
                                                        <asp:Label ID="Label1" runat="server" CssClass='<%# IIF(IsWrongType(Eval("CostCenter"), Eval("LineType")), "ui-state-error ui-corner-all","") %>' ToolTip='<%# IIF(IsWrongType(Eval("CostCenter"), Eval("LineType")),Translate("lblWrongType"),"") %>' Text='<%# GetLocalTypeName(Eval("AP_Staff_RmbLineType.LineTypeId") ) & If(IsMileageType(Eval("LineType")), " " & GetMileageString(If(Eval("Mileage"), 0), If(Eval("Spare3"), "0")) ,"") %>' Visible=<%# Eval("GrossAmount")>=0 %>></asp:Label>
                                                        <asp:Panel runat="server">
                                                            <asp:Label ID="lblToFrom" runat="server" CssClass=<%# "second_line " & if(IsAccounts(),"finance","") %>  Visible=<%# If(TypeHasOriginAndDestination(Eval("LineType")),"True","False") %> Text=<%# If(Eval("Spare4") IsNot Nothing And Eval("Spare5") IsNot Nothing, Left(Eval("Spare4"),9) & " - " & Left(Eval("Spare5"),9),"") %>></asp:Label>
                                                            <asp:Label ID="lbl2ndLine" runat="server" CssClass=<%# "second_line " & if(IsAccounts(),"finance","") %> Visible=<%# If(ShowSecondTypeRow(Eval("LineType")),"True","False") %> Text=<%# If(Eval("Spare5") IsNot Nothing , Eval("Spare5"),"") %>></asp:Label>
                                                        </asp:Panel>
                                                    </ItemTemplate>
                                                    <ItemStyle HorizontalAlign="Left" />
                                                </asp:TemplateField>

                                                <asp:TemplateField HeaderText="Comment" >
                                                    <EditItemTemplate>
                                                    </EditItemTemplate>
                                                    <ItemTemplate>
                                                        <asp:Label ID="lblComment" runat="server" Text='<%#  Eval("Comment") & getSupplier(Eval("Supplier")) %>'></asp:Label>
                                                        <asp:Panel ID="pnlRemBal1" runat="server" Visible='<%# CanEdit(Eval("AP_Staff_Rmb.Status")) and IsAccounts()  %>'>
                                                            <asp:Label ID="lblTrimmedComment" runat="server" CssClass=<%# "second_line " & if(IsAccounts(),"finance","") %> Text='<%# GetLineComment(Eval("Comment"), Eval("OrigCurrency"), Eval("OrigCurrencyAmount"), Eval("ShortComment"))%>'></asp:Label>
                                                        </asp:Panel>

                                                    </ItemTemplate>
                                                    <FooterTemplate>
                                                        <asp:Label ID="lblTotal" runat="server" Font-Bold="True" Text="Total:"></asp:Label>
                                                        <asp:Panel ID="pnlRemBal1" runat="server" Visible='<%# Settings("ShowRemBal") = "True" %>'>
                                                            <asp:Label ID="lblRemainingBalance" runat="server" CssClass=<%# "second_line " & if(IsAccounts(),"finance","") %> Font-Italic="true" ></asp:Label>
                                                        </asp:Panel>
                                                    </FooterTemplate>
                                                    <ItemStyle HorizontalAlign="Left" />
                                                    <FooterStyle HorizontalAlign="Right" />
                                                </asp:TemplateField>

                                                <asp:TemplateField HeaderText="Amount" SortExpression="Amount" ItemStyle-Width="75px">
                                                    <EditItemTemplate>
                                                    </EditItemTemplate>
                                                    <ItemTemplate>
                                                        <asp:Label ID="lblAmount" runat="server" CssClass='<%# IIF(Eval("LargeTransaction"), "ui-state-highlight ui-corner-all","") %>' ToolTip='<%# IIF(Eval("LargeTransaction"),Translate("LargeTransaction"),"") %>' Text='<%#  Eval("GrossAmount", "{0:F2}") & IIF(Eval("Taxable")=True, "*", "") %>'></asp:Label>

                                                        <asp:Panel ID="pnlCur" runat="server" Visible='<%# Not String.IsNullOrEmpty(Eval("OrigCurrency")) And Eval("OrigCurrency") <> StaffBrokerFunctions.GetSetting("AccountingCurrency", PortalId)%>'>
                                                            <asp:Label ID="lblCur" runat="server" Text='<%# Eval("OrigCurrency") & Eval("OrigCurrencyAmount", "{0:F2}")%>' 
                                                                CssClass='<%# "second_line " & if(IsAccounts(),"finance","") & IF(Eval("ExchangeRate") isNot Nothing, If(differentExchangeRate(Eval("ExchangeRate"), If(Eval("GrossAmount")<>0,Eval("OrigCurrencyAmount")/Eval("GrossAmount"),Eval("ExchangeRate"))), "highlight",""),"") %>' 
                                                                ToolTip='<%# If(Eval("ExchangeRate") isNot Nothing, If(differentExchangeRate(Eval("ExchangeRate"), If(Eval("GrossAmount")<>0,Eval("OrigCurrencyAmount")/Eval("GrossAmount"),Eval("ExchangeRate"))), Translate("DifferentExchangeRate"), ""),"")%>'></asp:Label>
                                                        </asp:Panel>
                                                    </ItemTemplate>
                                                    <FooterTemplate>
                                                        <asp:Label ID="lblTotalAmount" runat="server" CssClass="FormTotal" Text='<%# StaffBrokerFunctions.GetSetting("Currency", PortalId) & GetTotal(-1).ToString("F2") %>'></asp:Label>
                                                        <asp:Panel ID="pnlRemBal2" runat="server" Visible='<%# Settings("ShowRemBal") = "True"%>'>
                                                            <asp:Label ID="lblRemainingBalanceAmount" runat="server" Font-Size="xx-small" Text=''></asp:Label>
                                                        </asp:Panel>
                                                    </FooterTemplate>
                                                    <ItemStyle HorizontalAlign="Right" />
                                                    <FooterStyle HorizontalAlign="Right" />
                                                </asp:TemplateField>

                                                <asp:TemplateField HeaderText="Receipt" ItemStyle-Width="20px">
                                                    <ItemTemplate>
                                                        <%# If(Not Eval("Receipt"), "<img src='/Icons/Sigma/no_receipt_32x32.png' width=20 alt='none' title='no receipt' />", ElectronicReceiptTags(Eval("RmbLineNo"))) %>
                                                    </ItemTemplate>
                                                    <ItemStyle HorizontalAlign="Center" />
                                                </asp:TemplateField>
                                                
                                                <asp:TemplateField HeaderText="" ItemStyle-Width="10px" ItemStyle-Wrap="false">
                                                    <EditItemTemplate>
                                                    </EditItemTemplate>
                                                    <ItemTemplate>
                                                        <asp:LinkButton ID="LinkButton5" runat="server" CommandName="myEdit" Visible='<%# (Eval("GrossAmount")>0) and CanEdit(Eval("AP_Staff_Rmb.Status"))  %>'
                                                            CommandArgument='<%# Eval("RmbLineNo") %>' resourcekey="Edit"></asp:LinkButton>
                                                        <asp:LinkButton ID="LinkButton4" runat="server" CommandName="myDelete" Visible='<%# CanEdit(Eval("AP_Staff_Rmb.Status")) %>' CssClass="confirm"
                                                            CommandArgument='<%# Eval("RmbLineNo") %>' resourcekey="Delete"></asp:LinkButton>
                                                        <asp:Panel ID="Accounts" runat="server" Visible='<%# (CInt(Eval("AP_Staff_Rmb.Status"))<>StaffRmb.RmbStatus.Draft and CInt(Eval("AP_Staff_Rmb.Status"))<>StaffRmb.RmbStatus.Paid and CInt(Eval("AP_Staff_Rmb.Status"))<>StaffRmb.rmbStatus.Processing and CInt(Eval("AP_Staff_Rmb.Status"))<>StaffRmb.rmbStatus.DownloadFailed and CInt(Eval("AP_Staff_Rmb.Status"))<>StaffRmb.rmbStatus.PendingDownload)  and IsAccounts()  %>'>
                                                            <asp:LinkButton ID="LinkButton6" runat="server" CommandName="mySplit"
                                                                CommandArgument='<%# Eval("RmbLineNo") %>' resourcekey="Split"></asp:LinkButton>
                                                            <asp:LinkButton ID="LinkButton7" runat="server" CommandName="myDefer" ToolTip="Moves this transaction to a new 'Pending' Reimbursement."
                                                                CommandArgument='<%# Eval("RmbLineNo") %>' resourcekey="Defer" Text="Defer"></asp:LinkButton>

                                                        </asp:Panel>


                                                    </ItemTemplate>
                                                    <ItemStyle HorizontalAlign="Left" />
                                                </asp:TemplateField>
                                                <asp:TemplateField HeaderText="" ItemStyle-Width="10px" ItemStyle-Wrap="false">
                                                    <EditItemTemplate>
                                                    </EditItemTemplate>
                                                    <ItemTemplate>
                                                    </ItemTemplate>
                                                    <ItemStyle HorizontalAlign="Left" />
                                                </asp:TemplateField>
                                            </Columns>
                                            <FooterStyle CssClass="ui-widget-header dnnGridFooter acGridHeader" />
                                            <HeaderStyle CssClass="ui-widget-header dnnGridHeader acGridHeader" />
                                            <PagerStyle CssClass="dnnGridPager" />
                                            <SelectedRowStyle CssClass="dnnFormError" />
                                        </asp:GridView>

                                        <asp:Panel ID="pnlTaxable" runat="server" Visible="false" >
                                            <div style="float:left">
                                                <asp:Label ID="Label28" runat="server" Font-Italic="true" resourcekey="TaxableHint"></asp:Label>
                                            </div>
                                        </asp:Panel>
                                        <asp:LinkButton ID="btnDownload" runat="server">
                                            <div style="vertical-align: middle; float: right; padding-top: 8px; padding-bottom: 2px; font-size:11px">
                                                <img src="/DesktopModules/AgapeConnect/StaffRmb/Images/Excel_icon.gif" alt="" />
                                                <asp:Label ID="lblDownload" runat="server" resourcekey="btnDownload"></asp:Label>
                                            </div>
                                            <div style="clear: both;">
                                            </div>
                                        </asp:LinkButton>
                                    </div>
                                    <div style="clear:both;"></div>
                                    <div style="float:left; margin-left:20px">
                                        <asp:Button ID="addLinebtn2" runat="server" resourcekey="btnAddExpenseItem" class="aButton go" />
                                        <asp:Panel ID="pnlAdvance" runat="server" Visible="false" style="display:inline-block" >
                                            <input type="button" ID="btnClearAdvance" value='<%=Translate("btnClearAdvance") %>' class="aButton" onclick="showClearAdvancePopup();" />
                                            (<asp:Label runat="server" resourcekey="lblOutstandingAdvances" font-size="Smaller" font-weight="bold"/>
                                            <asp:Label ID="lblOutstandingAdvanceAmount" runat="server" font-size="Smaller" font-weight="bold" value="$0.00"/>)
                                        </asp:Panel>
                                    </div>
                                    <div style="float:right; margin-right:20px">
                                        <asp:Button ID="btnPrint" runat="server" resourcekey="btnPrint" class="aButton" />
                                        <asp:Button ID="btnSubmit" runat="server" resourcekey="btnSubmit" class="aButton" visible="false"/>
                                        <asp:Button ID="btnReject" runat="server" resourcekey="btnReject" class="aButton" Visible="false" />
                                        <asp:Button ID="btnApprove" runat="server" resourcekey="btnApprove" class="aButton" visible="false"/>
                                        <asp:Button ID="btnProcess" runat="server" resourcekey="btnProcess" class="aButton" onClientClick="showPostDataDialog()" visible="false"/>
                                        <asp:Button ID="btnUnProcess" runat="server" resourcekey="btnUnProcess" class="aButton" visible="false"/>
                                    </div>
                                    <%-- <button class="Excel" title="Download" >
                                        <asp:Label ID="Label3" runat="server" Text="Download"></asp:Label>
                                    </button>--%>
                                    <br />
                                    <br />
                                    <div id="errorSection" style="margin-top:15px;>
                                        <fieldset id="pnlError" runat="server" visible="false" style="margin: 15px;color:darkred;">
                                            <legend>
                                                <asp:Label ID="Label44" runat="server" CssClass="AgapeH4" ResourceKey="lblErrorMessage"></asp:Label>
                                            </legend>
                                            <asp:Label ID="lblWrongType" runat="server" class="ui-state-error ui-corner-all" Style="padding: 3px; margin-top: 5px; display: block;" resourceKey="lblWrongTypes"></asp:Label>
                                            <asp:Label ID="lblErrorMessage" runat="server" class="ui-state-error ui-corner-all" Style="padding: 3px; margin-top: 5px; display: block;"></asp:Label>
                                        </fieldset>
                                        <div style="clear: both;" />
                                    </div>
                                </div>

                                <asp:LinqDataSource ID="RmbLineDS" runat="server" ContextTypeName="StaffRmb.StaffRmbDataContext"
                                    EnableDelete="True" OrderBy="RmbLineNo" TableName="AP_Staff_RmbLines" Where="RmbNo == @RmbNo"
                                    EnableInsert="True" EnableUpdate="True" EntityTypeName="">
                                    <WhereParameters>
                                        <asp:ControlParameter ControlID="hfRmbNo" Name="RmbNo" PropertyName="Value" Type="Int64" />
                                    </WhereParameters>
                                </asp:LinqDataSource>





                            </div>





                        </asp:Panel>

                        <asp:HiddenField ID="hfRmbNo" runat="server" />
                        <asp:HiddenField ID="staffInitials" runat="server" Value="" />
                    </ContentTemplate>
                    <Triggers>
                        <asp:PostBackTrigger ControlID="btnDownload" />
                        <asp:AsyncPostBackTrigger ControlID="btnCreate" />
                        <asp:AsyncPostBackTrigger ControlID="btnAddClearingItem" />
                    </Triggers>
                </asp:UpdatePanel>
            </td>
        </tr>
    </table>

    <div id="divNewItem" class="ui-widget" >
        <div>
            <asp:UpdatePanel ID="UpdatePanel2" runat="server">
                <ContentTemplate>
                    <div align="left">
                        <asp:Label ID="PopupTitle" runat="server" resourcekey="NewLineTitle" CssClass="AgapeH2"></asp:Label><br />
                        <br />
                        <div style="max-height:550px; overflow-y:auto; overflow-x:hidden;">
                            <table style="font-size: 9pt" width="100%">
                                <tr valign="top">
                                    <td style="white-space: nowrap; min-width:194px;">
                                        <b>
                                            <dnn:Label ID="Label4" runat="server" ControlName="ddlLineTypes" ResourceKey="LineTypes" />
                                        </b>
                                    </td>
                                    <td width="100%">
                                        <asp:DropDownList ID="ddlLineTypes" runat="server" DataTextField="LocalName" DataValueField="LineTypeId"
                                            AppendDataBoundItems="true" AutoPostBack="true">
                                        </asp:DropDownList>
                                        <asp:Label ID="lblIncType" runat="server" CssClass="ui-state-error ui-corner-all" Text="Incompatible Type" Visible="false"></asp:Label>

                                        <div id="manualCodes" runat="server" style="float: right;">
                                            <asp:DropDownList ID="ddlAccountCode" runat="server" Width="70px" DataSourceID="dsAccountCodes"
                                                DataTextField="DisplayName" DataValueField="AccountCode" Enabled="false">
                                            </asp:DropDownList>


                                            <asp:LinqDataSource ID="dsAccountCodes" runat="server" ContextTypeName="StaffRmb.StaffRmbDataContext"
                                                EntityTypeName="" Select="new (AccountCode,  AccountCode + ' ' + '-' + ' ' + AccountCodeName  as DisplayName )"
                                                TableName="AP_StaffBroker_AccountCodes" OrderBy="AccountCode" Where="PortalId == @PortalId">
                                                <WhereParameters>
                                                    <asp:ControlParameter ControlID="hfPortalId" DefaultValue="-1" Name="PortalId" PropertyName="Value"
                                                        Type="Int32" />
                                                </WhereParameters>
                                            </asp:LinqDataSource>
                                            <asp:TextBox ID="tbCostcenter" runat="server" Width="90px" Enabled="false">
                                            </asp:TextBox>
                                            <asp:LinqDataSource ID="dsCostCenters" runat="server" ContextTypeName="StaffBroker.StaffBrokerDataContext"
                                                EntityTypeName="" OrderBy="CostCentreCode" Select="new (CostCentreCode,CostCentreCode + ' ' + '-' + ' ' + CostCentreName as DisplayName)"
                                                TableName="AP_StaffBroker_CostCenters" Where="PortalId == @PortalId">
                                                <WhereParameters>
                                                    <asp:ControlParameter ControlID="hfPortalId" DefaultValue="-1" Name="PortalId" PropertyName="Value"
                                                        Type="Int32" />
                                                </WhereParameters>
                                            </asp:LinqDataSource>
                                        </div>

                                    </td>
                                </tr>
                            </table>
                            <asp:PlaceHolder ID="phLineDetail" runat="server"></asp:PlaceHolder>
                            <asp:Panel ID="pnlElecReceipts" runat="server" CssClass="electronic_receipts_panel" style="display: none;">
                            <table style="font-size: 9pt;">
                                <tr valign="top">
                                    <td width="150px;"><b>
                                        <dnn:Label ID="lblElectronicReceipts" ClientIDMode="static" runat="server"  ResourceKey="lblElectronicReceipts" />
                                    </b></td>
                                    <td>
                                  
                                     <iframe id="ifReceipt" class="ifreceipt" runat="server" src="" width="530"  >

                                     </iframe>
                                    </td>
                                </tr>


                            </table>
                            </asp:Panel>
                            <div style="width:100%;text-align:right; margin-top:10px;">
                                <asp:Button ID="btnCancelLine" runat="server" resourceKey="btnCancel" OnClientClick="closeNewItemPopup();" class="aButton" />
                                <asp:Button ID="btnSaveLine" runat="server" resourcekey="btnEnter" onclientclick="$(this).prop('disabled',true).addClass('aspNetDisabled');" CommandName="Save"  class="aButton" />
                            </div>
                            </div>
                        <fieldset id="pnlAccountsOptions" runat="server" class="accounts_options">
                            <legend>
                                <asp:Label ID="Label31" runat="server" resourcekey="AccountsOnly"></asp:Label></legend>
                            <table>
                                <tr>
                                    <td>
                                        <asp:Label ID="Label47" runat="server" resourcekey="ShortComment"></asp:Label>
                                    </td>
                                    <td style="font-family: 'Courier New';">
                                        <%= staffInitials.Value %>-<asp:TextBox ID="tbShortComment" runat="server" MaxLength="27" Width="200px"></asp:TextBox>
                                    </td>
                                </tr>
                                <tr>
                                    <td>
                                        <asp:Label ID="Label43" runat="server" resourcekey="OverideTax"></asp:Label>
                                    </td>
                                    <td>
                                        <asp:DropDownList ID="ddlOverideTax" CssClass='ddlTaxable' runat="server">
                                            <asp:ListItem Value="0" resourcekey="NonTaxable"></asp:ListItem>
                                            <asp:ListItem Value="1" resourcekey="Taxable"></asp:ListItem>
                                        </asp:DropDownList>
                                    </td>
                                </tr>


                                <asp:Panel ID="pnlVAT" runat="server">
                                    <tr>
                                        <td>
                                            <asp:Label ID="Label29" runat="server" resourcekey="RecoverVAT"></asp:Label>
                                        </td>
                                        <td>
                                            <asp:CheckBox ID="cbRecoverVat" runat="server" />
                                        </td>
                                    </tr>
                                    <tr id="pnlVatOveride" runat="server">
                                        <td>
                                            <asp:Label ID="Label30" runat="server" resourcekey="RecoverVATRate"></asp:Label>
                                        </td>
                                        <td>
                                            <asp:TextBox ID="tbVatRate" type="number" step="0.0001" runat="server" Width="50" ></asp:TextBox>
                                        </td>
                                    </tr>
                                </asp:Panel>
                            </table>
                        </fieldset>
                    </div>
                </ContentTemplate>
                <Triggers>
                    <asp:AsyncPostBackTrigger ControlID="ddlLineTypes" EventName="SelectedIndexChanged" />
                    <asp:AsyncPostBackTrigger ControlID="btnSaveLine" EventName="Click" />
                    <%--  <asp:AsyncPostBackTrigger ControlID="btnPrint"  EventName="Click" />--%>
                    <%--<asp:PostBackTrigger ControlID="btnPrint" />--%>
                    <%--  <asp:PostBackTrigger ControlID="btnDownloadBatch" />
                <asp:PostBackTrigger ControlID="btnSuggestedPayments" />--%>
                    <%--  <asp:PostBackTrigger ControlID="btnSaveLine" />--%>
                </Triggers>
            </asp:UpdatePanel>
            <asp:UpdateProgress ID="UpdateProgress1" runat="server" DisplayAfter="0" DynamicLayout="true"
                AssociatedUpdatePanelID="UpdatePanel2">
                <ProgressTemplate>
                    <asp:Image ID="updating1" ImageUrl="~/Images/progressbar2.gif" runat="server" />
                </ProgressTemplate>
            </asp:UpdateProgress>
        </div>
    </div>

    <div id="divNewRmb" class="ui-widget">
        <%-- New Rmb--%>
        <div>
            <asp:UpdatePanel ID="UpdatePanel3" runat="server">
                <ContentTemplate>
                    <div class="AgapeH2">
                        <asp:Label ID="Label32" runat="server" resourcekey="btnNew"></asp:Label>
                    </div>
                    <table width="100%">
                        <tr class="Agape_SubTitle">
                            <td width="60px">
                                <asp:Label ID="Label33" runat="server" resourcekey="NameThis"></asp:Label>
                            </td>
                            <td>
                                <asp:TextBox ID="tbNewYourRef" runat="server" Width="150px" title="This is a personal reference ID to help you identify this Reimbursement"></asp:TextBox>
                            </td>
                            <td width="70px">Charge To:
                            </td>
                            <td>
                                <asp:HiddenField ID="hfNewChargeTo" runat="server" value=""></asp:HiddenField>
                                <asp:TextBox ID="tbNewChargeTo" runat="server" title="What account would you like to be reimbursed from?">
                                </asp:TextBox>
                            </td>
                        </tr>
                        <tr class="Agape_SubTitle">
                            <td width="80px" >
                                <dnn:Label id="lblNewOnBehalfOf" runat="server" controlName="tbNewOnBehalfOf" resourceKey="lblOnBehalfOf"></dnn:Label>
                            </td>
                            <td colspan="2">
                                <asp:HiddenField ID="hfOnBehalfOf" runat="server" Value="" />
                                <asp:textbox ID="tbNewOnBehalfOf" runat="server"></asp:textbox>
                            </td>
                        </tr>
                    </table>
                    <table width="100%">
                        <tr valign="top">
                            <td style="font-size: 8pt; width: 33%;">
                                <fieldset>
                                    <legend class="AgapeH4">
                                        <asp:Label ID="Label34" runat="server" resourcekey="YourComments"></asp:Label></legend>
                                    <asp:TextBox ID="tbNewComments" runat="server" Height="100" TextMode="MultiLine" CssClass="prevent_resize"
                                        Width="100%"></asp:TextBox>
                                </fieldset>
                            </td>
                        </tr>
                    </table>
                    <div width="100%" style="text-align:right; margin-top:10px;">
                        <input id="btnCancel2" type="button" value='<%= Translate("btnCancel") %>' class="aButton" onclick="closeNewRmbPopup();" class="aButton" />
                        <asp:Button ID="btnCreate" runat="server" resourcekey="btnCreate" class="aButton" CssClass="aButton" OnClientClick="closeNewRmbPopup(); show_loading_spinner();" />
                    </div>
                </ContentTemplate>
            </asp:UpdatePanel>
        </div>

    </div>

    <div id="divInsufficientFunds" class="ui-widget">
        <%--Not Used?--%>
        <div>
            <br />
            <b>There is not enough money in the RC to cover this Reimbursement. Processing this expense may result in a negative account balance. Do you wish to
            continue?</b><br />
            <br />
            <div width="100%" align="center">
                <asp:ImageButton ID="ImageButton2" runat="server" OnClientClick="closeNSFPopup();"
                    ImageUrl="~/images/ButtonImages/Cancel.gif" onmouseover="this.src='../images/ButtonImages/Cancel_f2.gif';"
                    onmouseout="this.src='../images/ButtonImages/Cancel.gif';" class="aButton" AlternateText="Cancel"
                    ToolTip="Cancel" />
                <span onclick="closeNSFPopup()">
                    <asp:HyperLink ID="blockedLink" runat="server" Target="_blank" ImageUrl="~/images/ButtonImages/ContinueS.gif"></asp:HyperLink></span>
            </div>
        </div>
    </div>

    <asp:Label ID="lblDefatulSettings" runat="server" ForeColor="Red" resourcekey="DefaultSettings"></asp:Label>
    <div id="divWarningDialog" class="ui-widget" >
        <asp:UpdatePanel ID="WarningUpdatePanel" runat="server">
            <ContentTemplate>
                    <div class="AgapeH2">
                        <asp:Label ID="Label12" runat="server" resourcekey="Warning"></asp:Label>
                    </div>
                <h5><asp:Label ID="lblWarningLabel" runat="server"></asp:Label></h5>
                <br />
                <hr />
                <input id="btnAcknowledge" type="button" value='<%= Translate("btnOK")%>' onclick="closeWarningDialog();"
                        class="aButton" style="float:right" />
            </ContentTemplate>
        </asp:UpdatePanel>
    </div>

    <div id="divGetPostingData" class="ui-widget">
        <asp:UpdatePanel ID="PostDataDialog" runat="server">
            <ContentTemplate>
                    <div class="AgapeH2">
                        <asp:Label ID="Label10" runat="server" resourcekey="PostingData"></asp:Label>
                    </div>
                <table style="width:100%; padding:20px;">
                    <tr><td><asp:Label ID="lblCompany" runat="server" resourcekey="Company" /></td>
                        <td style="width:100%"><asp:DropDownList ID="ddlCompany" runat="server" AutoPostBack="True" /></td></tr>
                    <tr><td><asp:Label ID="lblPostingDate" runat="server" resourcekey="PostingDate" /></td>
                        <td><asp:TextBox ID="dtPostingDate" type="date" runat="server" /></td></tr>
                    <tr><td><asp:Label ID="lblBatchId" runat="server" resourcekey="BatchId" /></td>
                        <td><asp:TextBox ID="tbBatchId" runat="server" MaxLength="15" AutoCompleteType="none"/></td></tr>
                    <tr><td><asp:Label ID="lblPostingReference" runat="server" resourcekey="PostingReference" /></td>
                        <td><asp:TextBox ID="tbPostingReference" runat="server" /></td></tr>
                    <tr><td><asp:Label ID="lblInvoiceNumber" runat="server" resourcekey="InvoiceNumber" /></td>
                        <td><asp:TextBox ID="tbInvoiceNumber" runat="server" /></td></tr>
                    <tr><td><asp:Label ID="lblVendorId" runat="server" resourcekey="VendorId" /></td>
                        <td><asp:TextBox ID="tbVendorId" runat="server" CssClass="autocomplete" AutoPostBack="True"/></td></tr>
                    <tr><td><asp:Label ID="lblRemitTo" runat="server" resourcekey="RemitTo" /></td>
                        <td><asp:DropDownList ID="ddlRemitTo" runat="server" AutoPostBack="True" /></td></tr>
                </table>
                <table style="background-color:lightyellow; font-size:smaller; border:1px solid; box-shadow:3px 3px rgba(0,0,0,0.3); margin:20px; padding:10px;">
                    <tr><td><b><asp:Label runat="server" resourceKey="AddressOnFile" /></b> <asp:Label ID="lblAddressName" runat="server" Font-Italic="true"/></td></tr>
                    <tr><td>
                        <asp:Label id="lblAddressLine1" runat="server" /><br />
                        <asp:Label ID="lblAddressLine2" runat="server" /><br />
                        <asp:Label ID="lblCity" runat="server" />,&nbsp;<asp:Label ID="lblProvince" runat="server" />, 
                        <asp:Label ID="lblCountry" runat="server" /><br />
                        <asp:Label ID="lblPostalCode" runat="server" />
                        </td></tr>
                </table>
                <table style="width:100%">
                    <tr><td><input id="btnCancelPost" type="button" class="aButton" onclick="closePostDataDialog();" value="<%= Translate("btnCancel") %>" /></td>
                        <td><asp:button ID="btnSubmitPostingData" runat="server" resourcekey="btnOK" cssclass="aButton right" OnClientClick="show_loading_spinner()" AutoPostBack="True"/></td></tr>
                </table>

            </ContentTemplate>
        </asp:UpdatePanel>
    </div>

    <div id="divClearAdvancePopup" class="ui-widget">
        <asp:updatepanel ID="upClearAdvance" runat="server">
            <ContentTemplate>
                <fieldset>
                    <legend class="AgapeH4">
                        <asp:Label runat="server" resourcekey="lblClearAdvance" />
                    </legend>
                    <table width="100%">
                        <tr>
                            <td>
                                <asp:GridView ID="gvUnclearedAdvances" runat="server" AutoGenerateColumns="False"
                                            CellPadding="4" ForeColor="#333333" GridLines="Horizontal" Width="100%" showFooter="true">
                                    <Columns>
                                        <asp:TemplateField >
                                            <ItemTemplate>
                                                <asp:HiddenField ID="hfRmbNo" runat="server" Value='<%# Eval("RmbNo") %>'></asp:HiddenField>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField>
                                            <ItemTemplate>
                                                <asp:HiddenField ID="hfRmbLineNo" runat="server" Value='<%# Eval("RmbLineNo") %>'></asp:HiddenField>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Cost Center">
                                            <ItemTemplate>
                                                <asp:Label ID="lblCostCenter" runat="server" Text='<%# Eval("CostCenter") %>'></asp:Label>
                                            </ItemTemplate>
                                            <ItemStyle HorizontalAlign="Center" width="50px" />
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Advance Purpose">
                                            <ItemTemplate>
                                                <asp:Label ID="lblAdvanceComment" runat="server" Text='<%# Eval("Comment") %>'></asp:Label>
                                            </ItemTemplate>
                                            <ItemStyle HorizontalAlign="Left" width="150px" />
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Clearing Date" SortExpression="TransDate">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblAdvanceDate" runat="server" CssClass='<%# If( Eval("TransDate")<Today,"NormalRed","") %>' Text='<%# Eval("TransDate", "{0:d}") %>'></asp:Label>
                                                </ItemTemplate>
                                                <ItemStyle HorizontalAlign="Center" Width="50px" />
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Outstanding Amount">
                                            <ItemTemplate>
                                                <asp:Label ID="lblAdvanceBalance" runat="server" Text=<%# If(String.IsNullOrEmpty(Eval("Spare2")), "Not Specified", Eval("Spare2"))%>></asp:Label>
                                            </ItemTemplate>
                                            <ItemStyle HorizontalAlign="Right" Width="50px"  />
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Amount to clear">
                                            <ItemTemplate>
                                                <asp:TextBox ID="tbAdvanceClearing" type="number" step="0.01" runat="server" Text='<%# Bind("Spare3") %>' CssClass="clearAdvance" Style="text-align:right" value="0.00" 
                                                     onkeyup="updateClearingTotal();" onfocus="$(this).select();"></asp:TextBox>
                                            </ItemTemplate>
                                            <ItemStyle HorizontalAlign="Right" Width="50px"  />
                                            <FooterTemplate>
                                                <asp:Label ID="lblTotalAmount" runat="server" Font-Bold="True" Text="Total:"></asp:Label>
                                                <asp:Label ID="lblClearAdvanceTotal" runat="server"  ></asp:Label>
                                            </FooterTemplate>
                                            <FooterStyle HorizontalAlign="Right" Width="50px"  />
                                        </asp:TemplateField>
                                    </Columns>
                                </asp:GridView>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:Label ID="lblAdvanceClearError" runat="server" Font-Size="9pt" ForeColor="Red" />
                            </td>
                        </tr>
                    </table>
                    <br />
                    <asp:Button ID="btnAddClearingItem" runat="server" resourcekey="btnAddClearingItem" onClientClick="$(this).prop('disabled', true).addClass('aspNetDisabled'); show_loading_spinner();" class="aButton right" />
                    <input id="btnCancelClearAdvance" type="button" value='<%= Translate("btnCancel") %>' onclick="closeClearAdvancePopup();" class="aButton" />
                </fieldset>
            </ContentTemplate>
        </asp:updatepanel>
    </div>

    <div id="divSplitPopup" class="ui-widget">
        <asp:UpdatePanel ID="UpdatePanel9" runat="server">
            <ContentTemplate>
                <div align="center">
                    <fieldset>
                        <legend class="AgapeH4">
                            <asp:Label ID="Label35" runat="server" resourcekey="OriginalTrans"></asp:Label></legend>
                        <table width="100%">
                            <tr valign="middle">
                                <td width="100%">
                                    <asp:Label ID="lblOriginalDesc" runat="server" Width="100%"></asp:Label>
                                </td>
                                <td>
                                    <asp:Label ID="lblOriginalAmt" runat="server" Width="100px"></asp:Label>
                                </td>
                            </tr>
                        </table>
                    </fieldset>
                    <fieldset>
                        <legend class="AgapeH4">
                            <asp:Label ID="Label36" runat="server" resourcekey="SplitIno"></asp:Label></legend>
                        <asp:HiddenField ID="hfRows" runat="server" Value="1" />
                        <asp:HiddenField ID="hfSplitLineId" runat="server" Value="-1" />
                        <asp:Table ID="tblSplit" runat="server" Width="100%">
                            <asp:TableRow>
                                <asp:TableCell Width="100%">
                                    <asp:TextBox ID="tbSplitDesc" runat="server" Width="100%" CssClass="Description"></asp:TextBox>
                                </asp:TableCell>
                                <asp:TableCell>
                                    <asp:TextBox ID="tbSplitAmt" type="number" step="0.01" runat="server" Width="100px" CssClass="Amount"></asp:TextBox>
                                </asp:TableCell>
                            </asp:TableRow>
                        </asp:Table>
                        <div style="text-align: left; width: 100%;">
                            <asp:LinkButton ID="btnSplitAdd" runat="server" resourcekey="btnSplitAdd"></asp:LinkButton><br />
                        </div>
                    </fieldset>
                    <br />
                    <br />
                    <asp:Button ID="btnOK" runat="server" resourcekey="btnOK" class="aButton" OnClientClick="show_loading_spinner()" />
                    <input id="btnCancel1" type="button" value='<%= Translate("btnCancel") %>' onclick="closePopupSplit();"
                        class="aButton" />
                    <asp:Label ID="lblSplitError" runat="server" ForeColor="Red" resourcekey="SplitError"
                        Visible="false"></asp:Label>
                </div>
            </ContentTemplate>
            <Triggers>
                <asp:AsyncPostBackTrigger ControlID="btnSplitAdd" EventName="Click" />
                <asp:AsyncPostBackTrigger ControlID="btnOK" EventName="Click" />
            </Triggers>
        </asp:UpdatePanel>
    </div>

    <div id="divAccountWarning" class="ui-widget">
        <asp:Label ID="Label46" runat="server" Font-Bold="true" resourcekey="lblAccountWarning"></asp:Label>
        <br />
        <br />
        <div width="100%" style="text-align: center">
            <asp:Button ID="btnAccountWarningYes" runat="server" resourcekey="btnYes" class="aButton" />

            <input id="Button5" type="button" value='<%= Translate("btnNo")%>' onclick="closePopupAccountWarning();"
                class="aButton" />

        </div>
    </div>



</asp:Panel>


<div style="text-align: left">

    <asp:Label ID="lblMovedMenu" runat="server" Font-Size="XX-Small" Font-Italic="true" ForeColor="Gray" Text="If you are looking for Settings, Suggested Payments or Download Batched Transactions, these links have moved. Click the faint wrench/screwdriver icon at the top right corner of this module. "></asp:Label>

    <%--  <asp:PostBackTrigger ControlID="btnSaveLine" />--%>
    
</div>


<div id="divSuggestedPayments" class="ui-widget">
    <table border="0" cellpadding="10" cellspacing="0">
        <tr>
            <td>


                <table>
                    <tr>
                        <td>
                            <dnn:Label ID="lblSalaries" runat="server" ControlName="cbSalaries" ResourceKey="cbSalaries" />
                        </td>
                        <td>
                            <asp:CheckBox ID="cbSalaries" runat="server" /></td>
                    </tr>
                    <tr>
                        <td>
                            <dnn:Label ID="lblExpenses" runat="server" ControlName="cbExpenses" ResourceKey="cbExpenses" />
                        </td>
                        <td>
                            <asp:CheckBox ID="cbExpenses" runat="server" Checked="true" /></td>
                    </tr>
                    <tr>
                        <td>
                            <dnn:Label ID="Label45" runat="server" ControlName="ddlBankAccount" ResourceKey="lblBankAccount" />
                        </td>
                        <td>

                            <asp:DropDownList ID="ddlBankAccount" runat="server" Width="60px" DataSourceID="dsAccountCodes2"
                                DataTextField="DisplayName" DataValueField="AccountCode">
                            </asp:DropDownList>
                            <asp:LinqDataSource ID="dsAccountCodes2" runat="server" ContextTypeName="StaffRmb.StaffRmbDataContext"
                                EntityTypeName="" Select="new (AccountCode,  AccountCode + ' ' + '-' + ' ' + AccountCodeName  as DisplayName )"
                                TableName="AP_StaffBroker_AccountCodes" OrderBy="AccountCode" Where="PortalId == @PortalId &amp;&amp; AccountCodeType == @AccountCodeType">
                                <WhereParameters>
                                    <asp:ControlParameter ControlID="hfPortalId" DefaultValue="-1" Name="PortalId" PropertyName="Value"
                                        Type="Int32" />
                                    <asp:Parameter DefaultValue="1" Name="AccountCodeType" Type="Byte" />
                                </WhereParameters>
                            </asp:LinqDataSource>
                        </td>
                    </tr>
                </table>



                <br />
                <br />
                <div width="100%" style="text-align: center">
                    <asp:Button ID="btnSuggestedPayments" runat="server" resourcekey="btnDownload" class="aButton" OnClientClick="closeSuggestedPayments();" />

                    <input id="Button2" type="button" value='<%= Translate("btnCancel") %>' onclick="closeSuggestedPayments();"
                        class="aButton" />
                </div>

            </td>
            <td style="border-left: 1px dashed #AAA;">
                <iframe id="ifSugPay" width="300" height="169" 
                     frameborder="0" allowfullscreen="true"></iframe>

            </td>
        </tr>
    </table>
</div>
