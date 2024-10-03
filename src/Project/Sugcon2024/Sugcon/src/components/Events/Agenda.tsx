import React from 'react';
import InnerHTML from 'dangerously-set-html-content';
import {
  Field,
  GetServerSideComponentProps,
  withDatasourceCheck
} from '@sitecore-jss/sitecore-jss-nextjs';
import { ComponentProps } from 'lib/component-props';

interface Fields {
  SessionizeUrl: Field<string>;
}

type AgendaProps = ComponentProps & {
  fields: Fields;
  data?: string;
  error?: boolean;
};

const AgendaDefaultComponent = (props: AgendaProps): JSX.Element => (
    <div className={`component promo ${props.params.styles}`}>
      <div className="component-content">
        <span className="is-empty-hint">Agenda</span>
      </div>
    </div>
);

const AgendaComponent = (props: AgendaProps): JSX.Element => {
  console.log('Received data in component:', props.data); // Log data received in component

  if (props.error) {
    return <div>Failed to load...</div>;
  }

  if (!props.data) {
    return <div>Loading...</div>;
  }

  // Check if the data is valid HTML
  return (
      <div>
        <div dangerouslySetInnerHTML={{ __html: props.data }}></div> {/* Render it */}
      </div>
  );
};

export const getServerSideProps: GetServerSideComponentProps = async (rendering, layoutData, context) => {
  const api = rendering?.fields?.SessionizeUrl?.value;

  // If no API URL is provided, return empty props
  if (!api) {
    return {
    };
  }

  try {
    // Fetch the data from the API
    const res = await fetch(api);
    if (!res.ok) {
      throw new Error(`Failed to fetch data: ${res.statusText}`);
    }
    const data = await res.text();

    // Return the data as props
    return {
      data
    };
  } catch (error) {
    console.error('Error fetching data:', error);

    // Return an error prop to handle in the component
    return {
      error: true
    };
  }
};

export const Default = withDatasourceCheck()<AgendaProps>(AgendaComponent);
