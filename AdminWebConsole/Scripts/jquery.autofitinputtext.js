/*
* Plagiarize from
* Autogrow Textarea Plugin Version v3.0
* http://www.technoreply.com/autogrow-textarea-plugin-3-0
*
* With some modifications to make it work with input textbox.
*
* Date: November 27, 2012
*/

jQuery.fn.autoFit = function () {
    return this.each(function () {

        var createMirror = function (inputtext) {
            jQuery(inputtext).after('<span class="autofit-inputtext-mirror"></span>');
            return jQuery(inputtext).next('.autofit-inputtext-mirror')[0];
        };

        var sendContentToMirror = function (inputtext) {
            mirror.innerHTML = inputtext.value.replace(/\n/g, '<br/>') + '.<br/>.';
            if (jQuery(inputtext).width() != jQuery(mirror).width() && jQuery(mirror).width() >= defaultWidth)
                jQuery(inputtext).width(jQuery(mirror).width());

            if (jQuery(mirror).width() <= defaultWidth)
                jQuery(inputtext).width(defaultWidth);
        };

        var growInputtext = function () {
            sendContentToMirror(this);
        };

        // Initial width of the inputtext
        var defaultWidth = jQuery(this).width();

        // Create a mirror
        var mirror = createMirror(this);

        // Style the mirror
        mirror.style.display = 'none';
        mirror.style.fontFamily = jQuery(this).css('font-family');
        mirror.style.fontSize = jQuery(this).css('font-size');
        mirror.style.padding = jQuery(this).css('padding');

        // Bind the inputtext's event
        this.onkeyup = growInputtext;

        // Fire the event for text already present
        sendContentToMirror(this);
    });
};