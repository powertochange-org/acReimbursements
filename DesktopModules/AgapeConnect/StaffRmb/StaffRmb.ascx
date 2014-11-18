<%@ Control Language="VB" AutoEventWireup="false" CodeFile="StaffRmb.ascx.vb" Inherits="DotNetNuke.Modules.StaffRmbMod.ViewStaffRmb" %>
<%@ Register TagPrefix="dnn" TagName="Label" Src="~/controls/LabelControl.ascx" %>

<%@ Register Src="Controls/Currency.ascx" TagName="Currency" TagPrefix="uc1" %>
<%@ Register Src="~/DesktopModules/AgapeConnect/StaffRmb/Controls/Currency.ascx" TagPrefix="dnn" TagName="Currency" %>

<script src="/js/gplus-youtubeembed.js" type="text/javascript"></script>

<script src="/js/jquery.numeric.js" type="text/javascript"></script>
<script src="/js/jquery.watermarkinput.js" type="text/javascript"></script>

<script src="/js/tree.jquery.js"></script>
<link rel="stylesheet" href="/js/jqtree.css" />


<script type="text/javascript">
    optimizeYouTubeEmbeds();

    var previous_menu_item = null;
    function selectMenuItem(menu_item) {
        deselectPreviousMenuItem();
        menu_item.style.fontWeight = 'bold';
        menu_item.style.fontSize = '9pt';
        $(menu_item).parent().next().children().css('visibility', 'visible');
        previous_menu_item = menu_item;
    }

    function deselectPreviousMenuItem() {
        if (previous_menu_item == null)
            return;
        previous_menu_item.style.fontWeight = 'normal';
        previous_menu_item.style.fontSize = '10pt';
        $(previous_menu_item).parent().next().children().css('visibility', 'hidden');
    }

    
    function loadRmb(rmbno) {
        var is_chrome = navigator.userAgent.toLowerCase().indexOf('chrome') > -1;
        if (is_chrome) {
            openInBackgroundTab("?rmbno="+rmbno);
        } else {
            window.open("?rmbno="+rmbno, "_blank");
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
        var date = control.datepicker('getDate');
        var expiry = new Date((new Date()).getTime() - <%= Settings("Expire") %>*24*3600000); //Number of days is set in Reimbursement settings
        if (date < expiry) {
            control.addClass("old_date");
            $("span#olddatetext").html("<-- <%= Translate("OldDate") %>");
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
                console.error('failure :'+b);
                $("#<%= tbVendorId.ClientID %>").prop('disabled', true);
                $("#<%= tbVendorId.ClientID%>").autocomplete({
                    source: ""
                });

            }
        });

    }

    function calculate_remaining_balance() {
        var result = "";
        var accBal = $("input[id$='StaffRmb_hfAccountBalance']:first").val();
        var formTot = $("span[id$='GridView1_lblTotalAmount']:last").text().replace("$","");
        if ((accBal == "") || (formTot == "")) {
            result = "unknown";
        } else {
            result = format_money(accBal - formTot);
        }
        $("span[id$='GridView1_lblRemainingBalance']:last").text(result);
    }

    function updatePerDiem(control, enabled) {
        control.attr('disabled', !enabled);
        if (enabled) {
            control.removeClass('aspNetDisabled');
        } else {
            control.addClass('aspNetDisabled');
            control.text="0"
        }
        calculatePerDiemTotal();
    }

    function calculatePerDiemTotal() {
        var total = 0;
        if ($('.pdbreakfast').is(':enabled')) { total += parseFloat($('.pdbreakfast').val()) };
        if ($('.pdlunch').is(':enabled')) { total += parseFloat($('.pdlunch').val()) };
        if ($('.pdsupper').is(':enabled')) { total += parseFloat($('.pdsupper').val()) };
        $('#tbAmount').text(total.toFixed(2));
    }

    function disableSubmitOnEnter(e)
    {
        var key;      
        if(window.event)
            key = window.event.keyCode; //IE
        else
            key = e.which; //firefox      

        return (key != 13);
    }

    function format_money(n) {
        decPlaces = 2,
        decSeparator = '.',
        thouSeparator = ',',
        sign = n < 0 ? "-" : "",
        i = parseInt(n = Math.abs(+n || 0).toFixed(decPlaces)) + "",
        j = (j = i.length) > 3 ? j % 3 : 0;
        return sign + (j ? i.substr(0, j) + thouSeparator : "") + i.substr(j).replace(/(\d{3})(?=\d)/g, "$1" + thouSeparator) + (decPlaces ? decSeparator + Math.abs(n - i).toFixed(decPlaces).slice(2) : "");
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
                $('#<%= hfCurOpen.ClientID %>').val("true")
            }

            $('.ddlReceipt').change(function() { 
                
                if( $('#' + this.id).val() == 2){
                   
                    $("#<%= pnlElecReceipts.ClientID%>").slideDown("slow");
                }
                else{
                    $("#<%= pnlElecReceipts.ClientID%>").slideUp("slow");
                }
            });

           
            // This is within the currency converter; anytime the exchange rate gets changed
            $('.equivalentCAD').keyup(function() { 
                calculateXRate(); 
                checkRecReq;
            });

            $('.equivalentCAD').blur(function() {
                var value = Number($('.equivalentCAD').val());
                if (value != null) {
                    $('.equivalentCAD').val(value.toFixed(2));
                }
            });

            $('.rmbAmount,.exchangeRate').keyup(function(event){
                var xRate = $(".exchangeRate").val();
                if (xRate == null) {
                    var amount = $('.rmbAmount').val();
                    $("input[name$='hfCADValue']").val(amount);
                } else {
                    console.log("setting exchange rate: "+xRate);
                    setXRate(xRate);
                    calculateEquivalentCAD();
                } 
            });

            $('.exchangeRate').blur(function() {
                var value = Number($('.exchangeRate').val());
                if (value != null) {
                    $('.exchangeRate').val(value.toFixed(4));
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
                close: function () {}
            });
            $("#divWarningDialog").parent().appendTo($("form:first"));

            $("#divSplitPopup").dialog({
                autoOpen: false,
                height: 400,
                width: 500,
                position: ['middle', 230],
                modal: true,
                title: '<%= Translate("SplitTransaction") %>',
                close: function () {
                    // allFields.val("").removeClass("ui-state-error");
                }
            });
            $("#divSplitPopup").parent().appendTo($("form:first"));

            $("#divNewItem").dialog({
                autoOpen: false,
                position:['middle', 120],
                //height: '<%= If(isAccounts(),760,700)%>',
                width: 780,
                modal: true,
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
                title: '<%= Translate("SuggestedPayments") %>',
                close: function () {
                    //  allFields.val("").removeClass("ui-state-error");
                }
            });
            $("#divSuggestedPayments").parent().appendTo($("form:first"));

            $('.aButton').button();
            $('.Excel').button({ icons: { primary: 'myExcel'} });


            var pickerOpts = {
                dateFormat: '<%= GetDateFormat() %>'
            };


            $('.datepicker').datepicker(pickerOpts);

            $('.numeric').numeric();
            $('.Description').Watermark('<%= Translate("Description") %>');
            $('.Amount').Watermark('<%= Translate("Amount") %>');
        };


        function setUpAccordion() {
            $("#accordion").accordion({
                header: "> div > h3",
                active: <%= getSelectedTab() %>,
                navigate: false
            });
        };

        function checkForMinistryAccount() {
            var account = $("#<%= tbChargeTo.ClientID %>").val();
            if (! account) return false;
            isMinistryAccount = (account.charAt(0)!='8' && account.charAt(0)!='9');
        };

        function setUpReceiptPreviews() {
            var url = ""
            $(".viewReceipt").hover(function(e){
                console.log(this.id);
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
            })
            $(".multiReceipt").hover(function(e){
                $("body").append("<div id='multi_notify' style='position:fixed; bottom:300px; right:50px'><center></br></hr><span class='AgapeH2'><%=Translate("MultipleReceipts")%></span></br><%=Translate("EditToView")%></center>");
                $("#multi_notify").show();
            },function(){
                $("#multi_notify").remove();
            })
            
        };

        function loadFinanceTrees() {
            $.getJSON(
                "/DesktopModules/AgapeConnect/StaffRmb/WebService.asmx/AllRmbs?portalid="+$('#<%= hfPortalId.ClientID %>').val()+
                            "&tabmoduleid="+$('#<%= hfTabModuleId.ClientID %>').val()+"&status=<%= StaffRmb.RmbStatus.Submitted%>",
                function(data) {
                    $("#treeSubmitted").tree({
                        data: data,
                        onCreateLi: function(node, $li) {
                            $li.find('.jqtree-title').not('.jqtree-title-folder').addClass('menu_link');
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
                        })
                }
            );
            $.getJSON(
                "/DesktopModules/AgapeConnect/StaffRmb/WebService.asmx/AllRmbs?portalid="+$('#<%= hfPortalId.ClientID %>').val()+
                            "&tabmoduleid="+$('#<%= hfTabModuleId.ClientID %>').val()+"&status=<%= StaffRmb.RmbStatus.Processing%>",
                function(data) {
                    $("#treeProcessing").tree({
                        data: data,
                        onCreateLi: function(node, $li) {
                            $li.find('.jqtree-title').not('.jqtree-title-folder').addClass('menu_link');
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
                        })
                }
            );
            $.getJSON(
                "/DesktopModules/AgapeConnect/StaffRmb/WebService.asmx/AllRmbs?portalid="+$('#<%= hfPortalId.ClientID %>').val()+
                            "&tabmoduleid="+$('#<%= hfTabModuleId.ClientID %>').val()+"&status=<%= StaffRmb.RmbStatus.Paid%>",
                function(data) {
                    $("#treePaid").tree({
                        data: data,
                        onCreateLi: function(node, $li) {
                            $li.find('.jqtree-title').not('.jqtree-title-folder').addClass('menu_link');
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
                        })
                }
            );
        }

        function enableDraggable() {
            $('.draggable').draggable({disabled:false});
        }

        function setUpConfirms() {
            $('.confirm').click(function() {
                return window.confirm("Are you sure?");
            })
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
            setUpConfirms();
            setUpHelpLink();
                         

            Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
                setUpMyTabs();
                setUpAutocomplete();
                checkForMinistryAccount();
                setUpReceiptPreviews();
                enableDraggable();
                setUpConfirms();
            });


        });


    } (jQuery, window.Sys));

    function GetAccountBalance(jsonQuery){
        $.getJSON(jsonQuery, function(json){
        var amountString = '<%=StaffBrokerFunctions.GetSetting("Currency", PortalId)  %>' + json ;
        $("#<%= lblAccountBalance.ClientId %>").html(amountString) ;
     });

 }

    function expandReceipt(){
        $("#<%= ifReceipt.ClientID %>").show();
    }


 function closeNewItemPopup()  {$("#divNewItem").dialog("close");}
 function closeNewRmbPopup() {$("#divNewRmb").dialog("close");}
 function closeNSFPopup() {$("#divInsufficientFunds").dialog("close");}
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
 }

    function resetSplitPopup() {
        $('#<%= btnOK.ClientID%>').prop('disabled', true);
        $('#<%= tblSplit.ClientID%>').find('tr:gt(0)').remove();
        $('#<%= tblSplit.ClientID%>').find('input').val('');
    }
    
 function showNewLinePopup()  {$("#divNewItem").dialog("open"); checkCur(); return false;}
 function showNewRmbPopup() {resetNewRmbPopup(); $("#divNewRmb").dialog("open"); return false; }
 function showNSFPopup() {$("#divInsufficientFunds").dialog("open"); return false; }
 function showPopupSplit() {resetSplitPopup(); $("#divSplitPopup").dialog("open"); return false; }
 function showWarningDialog() {$("#divWarningDialog").dialog("open"); return false; }
 function showAccountWarning() { $("#divAccountWarning").dialog("open"); return false; }
 function showPostDataDialog() { $("#divGetPostingData").dialog("open"); return false; }

     
 function showSuggestedPayments() {
      
     $('#ifSugPay').attr('src','https://www.youtube.com/embed/PEaTnZrpxfs?rel=0&wmode=transparent');
      
     $("#divSuggestedPayments").dialog("open"); 
     return false;

 }

 function checkCur(){
     var ac = '<%= StaffBrokerFunctions.GetSetting("AccountingCurrency", PortalId) %>';
     var currency = $("#<%= hfOrigCurrency.ClientID%>").val();
     var amount = Number($("#<%= hfOrigCurrencyValue.ClientID%>").val());
     if ($('.divCur').is(':visible')) {
         var exchangerate=1;
         if (currency == ac) {
             console.log("Canadian Currency, exchange rate set to 1.0000");
             setXRate(1.0000);
         } else {
             $('.ddlCur').val(currency);
             var CADAmount = $(".equivalentCAD").val();
             if (CADAmount>0) {
                 exchangerate = (amount/CADAmount).toFixed(4);
                 setXRate(exchangerate);
                 //calculateRevXRate();
                 console.log("checkCur(): "+amount+" "+currency+ " = " + CADAmount + " CAD @ " + exchangerate);
             } else {
                 console.log("ERROR: $CAD = 0");
             }
             
         }
    }
}


    // This function calculates the new exchange rate based on foreign amount and
    // canadian amount, and updates the amount field
    function calculateXRate() {
        var foreign = $('.rmbAmount').val();
        var cad = $('.equivalentCAD').val();
        var xRate = 1;
        if (cad.length>0 && parseFloat(cad) != 0) {
            xRate = foreign/cad;
        };
        xRate = Number(xRate).toFixed(4);
        $('.exchangeRate').val(xRate);
        console.log("calculateXRate(): "+xRate);
        setXRate(xRate);
        // Need to update some values for the backend:
        $("#<%= hfOrigCurrencyValue.ClientID%>").val(foreign);
        $("input[name$='hfCADValue']").val(cad);
    };

    function calculateEquivalentCAD() {
        var foreign = $('.rmbAmount').val();
        var xRate = $('#<%=hfExchangeRate.ClientID%>').val();
        if (xRate.length==0 || xRate<=0) {
            $(".equivalentCAD").val("0.00");
        } else {
            $('.equivalentCAD').val((foreign/xRate).toFixed(2));
        }
        $("#<%= hfOrigCurrencyValue.ClientID%>").val(foreign);
        $("input[name$='hfCADValue']").val($(".equivalentCAD").val());
        console.log("calculateEquivalentCAD(): " + foreign + " " + $("input[name$='hfOrigCurrency']").val() + " equals: " + $(".equivalentCAD").val() + " CAD");
        checkRecReq();
    };

    function currencyChange(selected_currency) {
        console.log("currencyChange(" + selected_currency + ");");
        var local_currency = $("input[name$='hfAccountingCurrency']").val();
        $(".ddlCur").val(selected_currency);
        $("[name$='hfOrigCurrency']").val(selected_currency);
        if (selected_currency != local_currency) {
            //foreign currency
            if ($("input[name$='hfCADValue']").val() != "0.00") {
                $(".equivalentCAD").val($("input[name$='hfCADValue']").val())
            } else {
                $(".equivalentCAD").val($(".rmbAmount").val())
            }
            calculateXRate();
            $(".curDetails").show();
            $(".ddlProvince").val("--");
            $('#<%= hfCurOpen.ClientID %>').val("true");
        } else {
            $("input[name$='hfExchangeRate']").val(1);
            $("input[name$='hfOrigCurrencyValue']").val($(".rmbAmount").val());
            $(".curDetails").hide();
            $('#<%= hfCurOpen.ClientID %>').val("false");
        }
    };

    function calculateRevXRate() {
        
        var xRate = $("#<%= hfExchangeRate.ClientId %>").attr('value');
        var inAmt=$('.rmbAmount').val() ;

        if(parseFloat(xRate) <0)
        {
            $('.currency').val('');
            $("#<%= hfOrigCurrencyValue.ClientID%>").attr('value',"");
         
            return;
        }
        
           
        if(inAmt.length>0){
            var value = (parseFloat(inAmt)/parseFloat(xRate) ).toFixed(2) ;
            $('.currency').val(value);
            $("#<%= hfOrigCurrencyValue.ClientID%>").attr('value',value);
            console.log("Currency Value:" + value);
        }
    };


    function setXRate(xRate){
        $("#<%= hfExchangeRate.ClientId %>").val(Number(xRate));
        $(".exchangeRate").val(xRate)
    };

    function checkRecReq(){
        try{
            
            var limit =  $("#<%= hfNoReceiptLimit.ClientID%>").attr('value');
            var Am=$("input[name$='hfCADValue']").val() ;
            //console.log(limit, Am);
            if( $('.ddlReceipt').val()==-1 && parseFloat(Am)>parseFloat(limit))
                $('.ddlReceipt').val(1);


            if(parseFloat(Am)>parseFloat(limit)){
                $('.ddlReceipt option[value="-1"]').attr("disabled", "disabled");
                //$('.ddlReceipt').attr("disabled", "disabled");
            }
            else 
            {
                $('.ddlReceipt option[value="-1"]').removeAttr("disabled");
            }

            // else   $('.ddlReceipt').removeAttr("disabled");
            

        }
        catch(err){

        }
    };


    function calculateTotal() {
        var total = 0.00;

        $(".Amount").each(function() {
            if (!isNaN(this.value) && this.value.length != 0) {total += parseFloat(this.value);}
        });
       
        var orig = $("#<%= lblOriginalAmt.ClientId %>").html();

        if(total== parseFloat(orig.substring(0,orig.Length)))
        {
            $("#<%= btnOK.ClientId %>").prop('disabled', false).removeClass('aspNetDisabled');
        }
        else
        {
            $("#<%= btnOK.ClientId %>").prop('disabled', true).addClass('aspNetDisabled');
        }
    };

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
                console.debug("SELECT: "+ui.item.value)
                $('#<%= hfChargeToValue.ClientID%>').val(ui.item.value);
                $('#<%= tbChargeTo.ClientID%>').val(ui.item.value).change();
            },
            change: function(event, ui) {
                if (!ui.item) {
                    console.debug("CHANGE: -null-")
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
                    console.debug("CHANGE: -null-")
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
                    console.info('users from cache');
                    response(cache[term]);
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
                console.debug("SELECT: "+ui.item.value)
                event.preventDefault();
                $('#<%= hfOnBehalfOf.ClientID%>').val(ui.item.value);
                $('#<%= tbNewOnBehalfOf.ClientID%>').val(ui.item.label).change();
            },
            change: function(event, ui) {
                if (!ui.item) {
                    console.debug("CHANGE: -null-")
                    $('#<%= hfOnBehalfOf.ClientID%>').val('');
                    $('#<%= tbNewOnBehalfOf.ClientID%>').val('');
                    alert("Please select the staff member again.  You must click on a name in the list, rather than just typing it.");
                }
            },
            minLength: 2
        });

    };

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
    <asp:HiddenField ID="staffInitials" runat="server" Value="" />
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
                                <asp:Label ID="Label8" runat="server" Font-Bold="true" ResourceKey="Processing"></asp:Label></a></h3>
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
                                            <asp:Label ID="ttlAccountBalance" runat="server" Style="margin-right: 3px; font-style: italic;  font-size:13px;" resourceKey="AccountBalance"></asp:Label>
                                            <asp:Label ID="lblAccountBalance" runat="server" Style="font-style: italic; font-size:13px;"></asp:Label>
                                            <asp:HiddenField ID="hfAccountBalance" runat="server" />
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
                                        <td class="hdrValue" valign="top">
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
                                        <td colspan="4">
                                            <table><tr class="Agape_SubTitle">
                                            <td class="hdrTitle" width="10%">
                                                <asp:Label ID="Label23" runat="server" resourcekey="YourRef"></asp:Label>
                                            </td>
                                            <td class="hdrValue">
                                                <asp:TextBox ID="tbYouRef" runat="server" Width="150px" onKeyPress="showSaveButton();"></asp:TextBox>
                                            </td>
                                            </tr>
                                            </table>
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
                                                <asp:Label ID="lblComments" runat="server" Height="60px" Visible="false"></asp:Label>
                                                <asp:TextBox ID="tbComments" runat="server" Height="55px" TextMode="MultiLine" Width="100%"
                                                    onKeyPress="showSaveButton();"></asp:TextBox>
                                            </fieldset>
                                        </td>
                                        <td colspan="2" style="font-size: 8pt; width: 33%;">
                                            <fieldset>
                                                <legend class="AgapeH4">
                                                    <asp:Label ID="Label26" runat="server" CssClass="hdrTitle" resourcekey="ApproversComments"></asp:Label></legend>
                                                <asp:Label ID="lblApprComments" runat="server" Height="60px"></asp:Label>
                                                <asp:TextBox ID="tbApprComments" runat="server" Height="35px" TextMode="MultiLine"
                                                    Width="100%" Visible="false"  onKeyPress="showSaveButton();"></asp:TextBox>
                                                <asp:CheckBox ID="cbApprMoreInfo" runat="server" AutoPostBack="true" resourcekey="btnMoreInfo" />
                                            </fieldset>
                                        </td>
                                        <td colspan="2" style="font-size: 8pt; width: 33%;">
                                            <fieldset>
                                                <legend class="AgapeH4">
                                                    <asp:Label ID="Label27" runat="server" CssClass="hdrTitle" resourcekey="AccountsComments"></asp:Label></legend>
                                                <asp:Label ID="lblAccComments" runat="server" Height="60px"></asp:Label>
                                                <asp:TextBox ID="tbAccComments" runat="server" Height="35px" TextMode="MultiLine" Width="100%"
                                                    Visible="false"  onKeyPress="showSaveButton();"></asp:TextBox>
                                                <asp:CheckBox ID="cbMoreInfo" runat="server" AutoPostBack="true" resourcekey="btnMoreInfo" />
                                            </fieldset>
                                        </td>
                                    </tr>
                                    <tr valign="top">
                                        <td colspan="6">
                                            <asp:Button ID="btnDelete" runat="server" resourcekey="btnDelete" class="aButton" OnClientClick='if (! window.confirm("Are you sure?")) return;' OnClick="btnDelete_Click" style="float:left"/>
                                            <asp:Button ID="btnSave" runat="server" resourcekey="btnSaved" class="aButton" style="float:right"/>
                                        </td>
                                    </tr>
                                </table>
                                <div class="rmbDataLines">
                                    <br />

                                    <asp:Label ID="lblTest" runat="server" Text="Label" Visible="false"></asp:Label>
                                    <div style="padding: 0 20px 0 20px;">
                                        <asp:GridView ID="GridView1" class="rmbDetails" runat="server" AutoGenerateColumns="False" DataKeyNames="RmbLineNo"
                                            CellPadding="4" ForeColor="#333333" GridLines="None" Width="100%" ShowFooter="True">
                                            <RowStyle CssClass="dnnGridItem" />
                                            <AlternatingRowStyle CssClass="dnnGridAltItem" />
                                            <Columns>
                                                <asp:TemplateField HeaderText="TransDate" SortExpression="TransDate">
                                                    <EditItemTemplate>
                                                        <asp:TextBox ID="TextBox2" runat="server" Text='<%# Bind("TransDate") %>'></asp:TextBox>
                                                    </EditItemTemplate>
                                                    <ItemTemplate>
                                                        <asp:Label ID="Label2" runat="server" CssClass='<%# IIF(Eval("OutOfDate"), "ui-state-highlight ui-corner-all","") %>' ToolTip='<%# IIF(Eval("OutOfDate"),Translate("OutOfDate"),"") %>' Text='<%# Bind("TransDate", "{0:d}") %>'></asp:Label>
                                                    </ItemTemplate>
                                                    <ItemStyle HorizontalAlign="Left" Width="50px" />
                                                </asp:TemplateField>

                                                <asp:TemplateField HeaderText="Extra" SortExpression="Spare1">
                                                    <EditItemTemplate>
                                                    </EditItemTemplate>
                                                    <ItemTemplate>
                                                        <asp:Label ID="lblExtra" runat="server" Text='<%# Eval("Spare1")%>'></asp:Label>
                                                    </ItemTemplate>
                                                    <ItemStyle HorizontalAlign="Center" />
                                                </asp:TemplateField>

                                                <asp:TemplateField HeaderText="Line Type" SortExpression="LineType" ItemStyle-Width="110px">
                                                    <EditItemTemplate>
                                                        <asp:TextBox ID="TextBox1" runat="server" Text='<%# Bind("LineType") %>'></asp:TextBox>
                                                    </EditItemTemplate>
                                                    <ItemTemplate>
                                                        <asp:Label ID="Label1" runat="server" CssClass='<%# IIF(IsWrongType(Eval("CostCenter"), Eval("LineType")), "ui-state-error ui-corner-all","") %>' ToolTip='<%# IIF(IsWrongType(Eval("CostCenter"), Eval("LineType")),Translate("lblWrongType"),"") %>' Text='<%# GetLocalTypeName(Eval("AP_Staff_RmbLineType.LineTypeId") ) & If(IsMileageType(Eval("LineType")), " " & GetMileageString(If(Eval("Mileage"), 0), If(Eval("Spare3"), "0")) ,"") %>'></asp:Label>
                                                        <asp:Panel runat="server">
                                                            <asp:Label ID="lblToFrom" runat="server" CssClass=<%# "second_line " & if(IsAccounts(),"finance","") %>  Visible=<%# If(TypeHasOriginAndDestination(Eval("LineType")),"True","False") %> Text=<%# If(Eval("Spare4") IsNot Nothing And Eval("Spare5") IsNot Nothing, Left(Eval("Spare4"),9) & " - " & Left(Eval("Spare5"),9),"") %>></asp:Label>
                                                            <asp:Label ID="lblPerDiemMeals" runat="server" CssClass=<%# "second_line " & if(IsAccounts(),"finance","") %> Visible=<%# If(isPerDiemType(Eval("LineType")),"True","False") and IsAccounts() %> Text=<%# If(Eval("Spare5") IsNot Nothing , Eval("Spare5"),"") %>></asp:Label>
                                                        </asp:Panel>
                                                    </ItemTemplate>
                                                    <ItemStyle HorizontalAlign="Left" />
                                                </asp:TemplateField>

                                                <asp:TemplateField HeaderText="Comment" SortExpression="Comment">
                                                    <EditItemTemplate>
                                                    </EditItemTemplate>
                                                    <ItemTemplate>
                                                        <asp:Label ID="lblComment" runat="server" Text='<%#  Eval("Comment") & getSupplier(Eval("Supplier")) %>'></asp:Label>
                                                        <asp:Panel ID="pnlRemBal1" runat="server" Visible='<%# CanEdit(Eval("AP_Staff_Rmb.Status")) and IsAccounts()  %>'>
                                                            <asp:Label ID="lblTrimmedComment" runat="server" CssClass=<%# "second_line " & if(IsAccounts(),"finance","") %> Text='<%# GetLineComment(Eval("Comment"), Eval("OrigCurrency"), Eval("OrigCurrencyAmount"), Eval("ShortComment"))%>'></asp:Label>
                                                        </asp:Panel>

                                                    </ItemTemplate>
                                                    <FooterTemplate>
                                                        <asp:Label ID="lblTotalAmount" runat="server" Font-Bold="True" Text="Total:"></asp:Label>
                                                        <asp:Panel ID="pnlRemBal1" runat="server" Visible='<%# Settings("ShowRemBal") = "True" %>'>
                                                            <asp:Label ID="lblRemainingBalance" runat="server" CssClass=<%# "second_line " & if(IsAccounts(),"finance","") %> Font-Italic="true" Text="Estimated Remaining Balance:"></asp:Label>
                                                        </asp:Panel>
                                                    </FooterTemplate>
                                                    <ItemStyle HorizontalAlign="Left" />
                                                    <FooterStyle HorizontalAlign="Right" />
                                                </asp:TemplateField>

                                                <asp:TemplateField HeaderText="Amount" SortExpression="GrossAmount" ItemStyle-Width="75px">
                                                    <EditItemTemplate>
                                                    </EditItemTemplate>
                                                    <ItemTemplate>
                                                        <asp:Label ID="lblAmount" runat="server" CssClass='<%# IIF(Eval("LargeTransaction"), "ui-state-highlight ui-corner-all","") %>' ToolTip='<%# IIF(Eval("LargeTransaction"),Translate("LargeTransaction"),"") %>' Text='<%#  Eval("GrossAmount", "{0:F2}") & IIF(Eval("Taxable")=True, "*", "") %>'></asp:Label>

                                                        <asp:Panel ID="pnlCur" runat="server" Visible='<%# Not String.IsNullOrEmpty(Eval("OrigCurrency")) And Eval("OrigCurrency") <> StaffBrokerFunctions.GetSetting("AccountingCurrency", PortalId)%>'>
                                                            <asp:Label ID="lblCur" runat="server" Text='<%# Eval("OrigCurrency") & Eval("OrigCurrencyAmount", "{0:F2}")%>' 
                                                                CssClass='<%# "second_line " & if(IsAccounts(),"finance","") & IF(Eval("ExchangeRate") isNot Nothing, If(differentExchangeRate(Eval("ExchangeRate"), Eval("OrigCurrencyAmount")/Eval("GrossAmount")), "highlight",""),"") %>' 
                                                                ToolTip='<%# If(Eval("ExchangeRate") isNot Nothing, If(differentExchangeRate(Eval("ExchangeRate"), Eval("OrigCurrencyAmount")/Eval("GrossAmount")), Translate("DifferentExchangeRate"), ""),"")%>'></asp:Label>
                                                        </asp:Panel>
                                                    </ItemTemplate>
                                                    <FooterTemplate>
                                                        <asp:Label ID="lblTotalAmount" runat="server" Text='<%# StaffBrokerFunctions.GetSetting("Currency", PortalId) & GetTotal(-1).ToString("F2") %>'></asp:Label>
                                                        <asp:Panel ID="pnlRemBal2" runat="server" Visible='<%# Settings("ShowRemBal") = "True"%>'>
                                                            <asp:Label ID="lblRemainingBalance" runat="server" Font-Size="xx-small" Text=''></asp:Label>
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
                                                        <asp:LinkButton ID="LinkButton5" runat="server" CommandName="myEdit" Visible='<%# CanEdit(Eval("AP_Staff_Rmb.Status"))  %>'
                                                            CommandArgument='<%# Eval("RmbLineNo") %>' resourcekey="Edit"></asp:LinkButton>
                                                        <asp:LinkButton ID="LinkButton4" runat="server" CommandName="myDelete" Visible='<%# CanEdit(Eval("AP_Staff_Rmb.Status")) %>' CssClass="confirm"
                                                            CommandArgument='<%# Eval("RmbLineNo") %>' resourcekey="Delete"></asp:LinkButton>
                                                        <asp:Panel ID="Accounts" runat="server" Visible='<%# (CInt(Eval("AP_Staff_Rmb.Status"))<>StaffRmb.RmbStatus.Paid and CInt(Eval("AP_Staff_Rmb.Status"))<>StaffRmb.rmbStatus.Processing and CInt(Eval("AP_Staff_Rmb.Status"))<>StaffRmb.rmbStatus.DownloadFailed and CInt(Eval("AP_Staff_Rmb.Status"))<>StaffRmb.rmbStatus.PendingDownload)  and IsAccounts()  %>'>
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
                                        <asp:Button ID="addLinebtn2" runat="server" resourcekey="btnAddExpenseItem" class="aButton" />
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
                    </ContentTemplate>
                    <Triggers>
                        <asp:PostBackTrigger ControlID="btnDownload" />
                        <asp:AsyncPostBackTrigger ControlID="btnCreate" />
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
                        <div style="max-height:450px; overflow-y:auto; overflow-x:hidden;">
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
                            <asp:Panel ID="pnlElecReceipts" runat="server" style="display: none;">
                            <table style="font-size: 9pt;">
                                <tr valign="top">
                                    <td width="150px;"><b>
                                        <dnn:Label ID="lblElectronicReceipts" runat="server"  ResourceKey="lblElectronicReceipts" />
                                    </b></td>
                                    <td>
                                  
                                     <iframe id="ifReceipt" class="ifreceipt" runat="server" src="" width="530" height="280" >

                                     </iframe>
                                    </td>
                                </tr>


                            </table>
                            </asp:Panel>
                            <div style="width:100%;text-align:right; margin-top:10px;">
                                <input type="button" value='<%= Translate("btnCancel") %>' onclick="closeNewItemPopup();" class="aButton" />
                                <asp:Button ID="btnSaveLine" runat="server" resourcekey="btnEnter" CommandName="Save"  class="aButton" />
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
                                            <asp:TextBox ID="tbVatRate" runat="server" Width="50" CssClass="numeric"></asp:TextBox>
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
                        <td><asp:TextBox ID="dtPostingDate" runat="server" Width="90px" class="datepicker" /></td></tr>
                    <tr><td><asp:Label ID="lblBatchId" runat="server" resourcekey="BatchId" /></td>
                        <td><asp:TextBox ID="tbBatchId" runat="server" AutoCompleteType="none"/></td></tr>
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
                                    <asp:TextBox ID="tbSplitAmt" runat="server" Width="100px" CssClass="Amount numeric"></asp:TextBox>
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
