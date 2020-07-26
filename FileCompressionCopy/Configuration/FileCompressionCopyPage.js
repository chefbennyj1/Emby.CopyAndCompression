define(["loading", "dialogHelper", "emby-checkbox", "emby-select", "emby-input"],
    function (loading, dialogHelper) {

        var pluginId = "D8B538E4-5579-4239-A251-721BC3AB4D9D";
        var autoOrganizeFolderLocationInput;
        var monitoredFolderLocationInput;
        var saveFolderOptionsButton;
        var completedItems; 

        function openDialog(element, view) {

            var path = "";
            var title = element.id === "monitoredFolderLocationDialog" ? "Monitored Folder" : "Emby Auto Organize Folder";
            var dlg = dialogHelper.createDialog({

                size         : "medium-tall",
                removeOnClose: !0,
                scrollY: !1

            });

            dlg.classList.add("formDialog");
            dlg.classList.add("ui-body-a");
            dlg.classList.add("background-theme-a");
            dlg.classList.add("directoryPicker");

            ApiClient.getJSON(ApiClient.getUrl("/Environment/Drives")).then(
                (result) => {

                    var html = "";
                    html += '<div class="formDialogHeader" style="display:flex">';
                    html += '<button is="paper-icon-button-light" class="btnCloseDialog autoSize paper-icon-button-light" tabindex="-1"><i class="md-icon"></i></button><h3 class="formDialogHeaderTitle">Select Path To ' + title + '</h3>';
                    html += '</div>';
                    html += '<div style="display: flex; align-items: center;">';
                    html += '<h1 style="margin: 0.5em 21.7em;">Folders</h>';
                    html += '</div>';

                    html += '<div class="formDialogContent" style="padding-top:2em; height:35em">';
                    html += '<div class="dialogContentInner dialog-content-centered scrollY" style="height:35em;">';
                    html += '<form style="margin:auto;">';
                    html += '<div class="inputContainer" style="display: flex; align-items: center;">';
                    html += '<div style="flex-grow:1;">';
                    html += '<div id="folderBrowser" class="results paperList" style="max-height: 30em; overflow-y: auto;">';

                    html += getDirectoryBrowserListItem({ Name: "Network", Path: "Network" });

                    result.forEach(drive => {
                        html += getDirectoryBrowserListItem(drive);
                    });

                    html += '</div>';
                    html += '</div>';
                    html += '</div>';
                    html += '</form>';
                    html += '</div>';
                    html += '</div>';
                    html += '<div class="formDialogFooter" style="padding-top:2em">';
                    html += '<button id="savePath" style="width: 50%;" is="emby-button" type="submit" class="raised button-submit block formDialogFooterItem emby-button">Ok</button>';
                    html += '</div>';

                    dlg.innerHTML = html;
                    dialogHelper.open(dlg);

                    dlg.querySelector('#folderBrowser').addEventListener('click',
                        (browser) => {
                            
                            var folderBrowser = dlg.querySelector('#folderBrowser');

                            folderBrowser.innerHTML = "";

                            switch (browser.target.closest('div.listItem').dataset.path) {
                            case "Back":
                                try {
                                    ApiClient.getJSON(ApiClient.getUrl("/Environment/DirectoryContents?Path=" +
                                        ApiClient.getUrl("/Environment/ParentPath?Path=" + path).then((r) => {

                                        r.forEach(drive => { 

                                                folderBrowser.innerHTML += getDirectoryBrowserListItem(drive);

                                            });
                                        })));
                                } catch (err) {
                                    ApiClient.getJSON(
                                    ApiClient.getUrl("/Environment/Drives")).then((r) => {
                                        folderBrowser.innerHTML += getDirectoryBrowserListItem({ Name: "Network", Path: "Network" });

                                                r.forEach(drive => {

                                                    folderBrowser.innerHTML +=
                                                        getDirectoryBrowserListItem(
                                                            drive);
                                                });
                                            });
                                    }
                                    break;
                                case "Network":
                                    ApiClient.getJSON(ApiClient.getUrl("/Environment/NetworkDevices")).then(
                                        (r) => {
                                            folderBrowser.innerHTML += getDirectoryBrowserListItem({ Name: "Back", Path: "Back" });
                                            r.forEach(drive => {

                                                folderBrowser.innerHTML += getDirectoryBrowserListItem(drive);

                                            });
                                        });
                                    path = browser.target.closest('div.listItem').dataset.path;
                                    break;
                                default:
                                    ApiClient.getJSON(ApiClient.getUrl("/Environment/DirectoryContents?Path=" +
                                        browser.target.closest('div.listItem').dataset.path +
                                        "&IncludeFiles=false&IncludeDirectories=true")).then(
                                            (r) => {
                                                folderBrowser.innerHTML += getDirectoryBrowserListItem({ Name: "Back", Path: "Back" });
                                                if (!r.length) {
                                                    folderBrowser.innerHTML += "<h1>Current Folder is Empty</h1>";
                                                }
                                                r.forEach(drive => {

                                                    folderBrowser.innerHTML += getDirectoryBrowserListItem(drive);

                                                });
                                        });
                                    path = browser.target.closest('div.listItem').dataset.path;
                                    break;
                            }
                        });

                    dlg.querySelector('.btnCloseDialog').addEventListener('click', () => {
                        dialogHelper.close(dlg);
                    });

                    dlg.querySelector('#savePath').addEventListener('click',
                        () => {
                            switch (element.id) {
                                case "monitoredFolderLocationDialog":
                                    view.querySelector('#monitoredFolderLocation').value    = decodeURI(path);
                                    break;
                                case "autoOrganizeFolderLocationDialog":
                                    view.querySelector('#autoOrganizeFolderLocation').value = decodeURI(path);
                                    break;
                            }
                            dialogHelper.close(dlg);
                        });
                });

        }

        function getDirectoryBrowserListItem(itemInfo) {
            var html = '';
            html += '<div class="listItem listItem-border" data-path=' + encodeURI(itemInfo.Path) + '>';
            html += '<div class="listItemBody two-line">';
            html += '<h3 class="listItemBodyText">';
            html += itemInfo.Name;
            html += '</h3>';
            html += '</div>';
            html += '<i class="md-icon" style="font-size:inherit;">arrow_forward</i>';
            html += '</div>';
            return html;
        }

        function timeSinceExtracted(completed) {

            var duration = Date.now() - new Date(completed)

            var minutes = parseInt((duration / (1000 * 60)) % 60)
            var hours   = parseInt((duration / (1000 * 60 * 60)) % 24);

            hours   = (hours < 10)   ? "0" + hours   : hours;
            minutes = (minutes < 10) ? "0" + minutes : minutes;
              
            return hours + " hours " + minutes + " minutes ago";
        }
             
        function getCompletedTasksHtml(extractionInfo) {

            
            var html = '';

            html += '<td data-title="Name" class="detailTableBodyCell fileCell">'      + extractionInfo.Name                          + '</td>';
            html += '<td data-title="Complete" class="detailTableBodyCell fileCell">'  + timeSinceExtracted(extractionInfo.completed) + '</td>';
            html += '<td data-title="Extention" class="detailTableBodyCell fileCell">' + extractionInfo.extension                     + '</td>';
            html += '<td data-title="Size" class="detailTableBodyCell fileCell">'      + extractionInfo.size                          + '</td>';
            html += '<td data-title="Move Type" class="detailTableBodyCell fileCell">' + extractionInfo.CopyType                      + '</td>';
            
            return html;
        }
        
        function loadPageData(view, config) {

            if (config.MonitoredFolder) {
                monitoredFolderLocationInput.value    = config.MonitoredFolder;
                autoOrganizeFolderLocationInput.value = config.EmbyAutoOrganizeFolderPath;
            }

            if (config.CompletedItems) {
                completedItems = view.querySelector('#completedItems');
                config.CompletedItems.forEach((item) => {
                    completedItems.innerHTML += getCompletedTasksHtml(item);
                });
            }
        }

        function loadConfig(view) {
            ApiClient.getPluginConfiguration(pluginId).then(
                (config) => {
                    loadPageData(view, config);
                    loading.hide();
                });
        }

        return function (view) {

            view.addEventListener('viewshow',
                () => {

                    loadConfig(view);

                    saveFolderOptionsButton         = view.querySelector('#saveFolderOptions');
                    autoOrganizeFolderLocationInput = view.querySelector('#autoOrganizeFolderLocation');
                    monitoredFolderLocationInput    = view.querySelector('#monitoredFolderLocation');

                    saveFolderOptionsButton.addEventListener('click',
                        () => {
                            loading.show();
                            ApiClient.getPluginConfiguration(pluginId).then(function (config) {

                                config.MonitoredFolder            = monitoredFolderLocationInput.value;
                                config.EmbyAutoOrganizeFolderPath = autoOrganizeFolderLocationInput.value;

                                ApiClient.updatePluginConfiguration(pluginId, config).then(function (result) {
                                    Dashboard.processPluginConfigurationUpdateResult(result);
                                });

                            });
                            loading.hide();
                        });
                });


            view.querySelector('#monitoredFolderLocationDialog').addEventListener('click',
                (e) => {
                    e.preventDefault();
                    openDialog(e.target.closest('button'), view);
                });

            view.querySelector('#autoOrganizeFolderLocationDialog').addEventListener('click',
                (e) => {
                    e.preventDefault();
                    openDialog(e.target.closest('button'), view);
                });

        }
    })