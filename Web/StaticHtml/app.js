/* globals CodeMirror:false */

$(function() {
  'use strict';

  var $loader = $('.loader');
  $loader.width($loader.height() + 'px');

  var editor = CodeMirror.fromTextArea($('#code textarea')[0], {
    mode:        'text/x-csharp',
    lineNumbers: true,
    indentUnit:  4
  });
  var decompiled = CodeMirror.fromTextArea($('#decompiled textarea')[0], {
    mode: 'text/x-csharp',
    readOnly: true
  });
  var lastHash;

  start();

  function start() {
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
    $loader.show();
    var code = editor.getValue();
    $.ajax("api/compilation", {
      method: 'POST',
      data: code,
      contentType: 'text/x-csharp'
    }).done(function(result) {
      clearErrors();
      decompiled.setValue(result);
      $loader.hide();
    }).fail(function(xhr) {
      reportErrors(xhr.responseText);
      $loader.hide();
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