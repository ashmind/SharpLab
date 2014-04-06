/* globals CodeMirror:false, config:false */

$(function() {
  'use strict';

  var editor;
  var decompiled;

  var lastHash;

  start();

  function start() {
    editor = CodeMirror.fromTextArea($('#code textarea')[0], {
      mode:        'text/x-csharp',
      lineNumbers: true,
      indentUnit:  4
    });
    
    decompiled = CodeMirror.fromTextArea($('#decompiled textarea')[0], {
      mode:     'text/x-csharp',
      readOnly: true
    });

    if (!tryLoadUrl())
      editor.setValue(getDefaultValue());

    var throttledSaveUrl = $.debounce(100, saveUrl);
    var throttledUpdateFromServer = $.debounce(600, updateFromServer);
    editor.on('change', function() {
      throttledSaveUrl();
      throttledUpdateFromServer();
    });
    saveUrl();
    updateFromServer();
    
    $(window).hashchange(function() { tryLoadUrl(); });
  }
  
  function saveUrl() {
    var hash = LZString.compressToBase64(editor.getValue());
    lastHash = hash;
    window.location.hash = hash;
  }
  
  function tryLoadUrl() {
    var hash = window.location.hash;
    if (!hash)
      return false;

    hash = hash.replace(/^#/, '');
    if (!hash || hash === lastHash)
      return false;

    var value;
    lastHash = hash;
    try {
      value = LZString.decompressFromBase64(hash);
    }
    catch (e) {
      return false;
    }

    editor.setValue(value);
    return true;
  }
  
  function getDefaultValue() {
    var value = $(editor.getWrapperElement())
                   .siblings('script[data-default]')
                   .text()
                   .trim();
    var lines = value.split(/[\r\n]+/g);
    var indent = lines[lines.length - 1].match(/^\s*/)[0];
    return value.replace(new RegExp(indent, 'g'), '');
  }
    
  function updateFromServer() {
    var code = editor.getValue();
    $.ajax("api/compilation", {
      method: 'POST',
      data: code,
      contentType: 'text/x-csharp'
    }).done(function(result) {
      clearErrors();
      decompiled.setValue(result);
    }).fail(function(xhr) {
      reportErrors(xhr.responseText);
    });
  }
  
  function reportErrors(errors) {
    $('header').addClass('error');
    decompiled.setValue(errors);
  }
  
  function clearErrors() {
    $('header').removeClass('error');
  }
});